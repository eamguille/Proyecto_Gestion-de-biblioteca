﻿using sistema_gestion_biblioteca.Controlador;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace sistema_gestion_biblioteca.Vista
{
    public partial class FrmGestionDevoluciones : Form
    {
        private devolucionControlador obj_controlador;
        private prestamoControlador obj_prestamo_controlador;
        private usuarioControlador obj_usuario_controlador;
        private libroControlador obj_libro_controlador;

        public FrmGestionDevoluciones()
        {
            InitializeComponent();
            obj_controlador = new devolucionControlador();
            obj_prestamo_controlador = new prestamoControlador();
            obj_usuario_controlador = new usuarioControlador();
            obj_libro_controlador = new libroControlador();
            ActualizarDataGrid();
        }

        private void FrmGestionDevoluciones_Load(object sender, EventArgs e)
        {
            ActualizarDataGrid();
            cargarCmbLibros();
            dtFechaDevolu.Format = DateTimePickerFormat.Custom;
            dtFechaDevolu.CustomFormat = " ";
            cargarNombresHeaders();

            dgDevoluciones.ReadOnly = true;
        }
        private BindingSource enlaceDatos = new BindingSource();

        private int index_tabla = -1;

        // METODOS DE SCRUD
        // Variable para evitar bucles de actualización infinita entre ComboBoxes
        private bool isUpdating = false;

        void cargarNombresHeaders()
        {
            dgDevoluciones.Columns[0].HeaderText = "Título del libro";
            dgDevoluciones.Columns[1].HeaderText = "Corrreo del usuario";
            dgDevoluciones.Columns[2].HeaderText = "Fecha de devolucion";
            dgDevoluciones.Columns[3].HeaderText = "Monto por retraso";
            dgDevoluciones.Columns[4].HeaderText = "Comentario";
        }

        void cargarCmbLibros()
        {
            cmbLibro.DropDownStyle = ComboBoxStyle.DropDownList;
            var lista = obj_libro_controlador.obtenerListaLibros();

            if (lista != null && lista.Count > 0)
            {
                var pos = lista.Select(elemento => elemento.titulo_libro).ToList();
                pos.Insert(0, "Selecciona el libro");
                cmbLibro.DataSource = pos;
                cmbLibro.SelectedIndex = 0;

                cmbLibro.SelectedIndexChanged += (s, ev) =>
                {
                    if (cmbLibro.SelectedIndex == 0)
                    {
                        cmbLibro.SelectedIndex = -1;
                        return;
                    }

                    // Vinculamos la seleccion del libro con el ultimo usuario que haya hecho un prestamo
                    string libroSeleccionado = cmbLibro.SelectedItem.ToString();
                    cargarCmbUltimoUsuario(libroSeleccionado);
                };
            }
        }

        void cargarCmbUltimoUsuario(string libro)
        {
            // Agregamos el libro para verificar su estado
            var libroSeleccionado = obj_libro_controlador.obtenerListaPorLibro(libro);

            if (libroSeleccionado != null)
            {
                // Verificamos si el libro ha sido prestado o no
                if (libroSeleccionado.estado_libro == "No Disponible")
                {
                    // Agregamos el ultimo prestamo que se ha realizado
                    var ultimoPrestamo = obj_prestamo_controlador.obtenerUltimoPrestamoPorLibro(libro);

                    if (ultimoPrestamo != null)
                    {
                        cmbUsuario.Items.Clear();
                        cmbUsuario.Items.Add(ultimoPrestamo.email_usuario);
                        cmbUsuario.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show("No se han realizado prestamos de este libro", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cmbUsuario.Items.Clear();
                    }
                } else if (libroSeleccionado.estado_libro == "Disponible")
                {
                    MessageBox.Show($"El libro {libroSeleccionado.titulo_libro} no ha sido prestado, Esta disponible para prestar", "Mensaje de Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            else
            {
                // Si el libro no existe
                MessageBox.Show("No se encontró el libro seleccionado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbUsuario.Items.Clear();
            }
        }


        void ActualizarDataGrid()
        {
            var listaPrestamos = obj_controlador.obtenerDevoluciones();
            enlaceDatos.DataSource = listaPrestamos;
            dgDevoluciones.DataSource = enlaceDatos;
        }

        void guardarDevolucion()
        {
            try
            {
                if (cmbUsuario.SelectedItem == null)
                {
                    MessageBox.Show("El campo de correo no puede estar vacio.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DateTime fechaDev;
                if (!DateTime.TryParse(dtFechaDevolu.Text, out fechaDev))
                {
                    MessageBox.Show("Por favor, ingresa una fecha valida.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (fechaDev > DateTime.Now)
                {
                    MessageBox.Show("La fecha ingresada no puede ser mayor a la actual.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                validarFechaMontoDevolucion();

                // Actualizamos el estado del libro nuevamente a "Disponible"
                string nombre_libro = cmbLibro.SelectedItem.ToString();
                string libroISBN = buscarISBNPorNombreLibro(nombre_libro);

                // Actualizamos el estado del prestamo a Devuelto
                string correo_usuario = cmbUsuario.SelectedItem.ToString();

                bool guardado = obj_controlador.agregarDevolucion(cmbLibro.Text, cmbUsuario.Text, dtFechaDevolu.Text, lblMonto.Text, txtComentario.Text);
                if (guardado)
                {
                    bool estado_libro_actualizar = obj_libro_controlador.actualizarEstadoLibro(libroISBN, "Disponible");
                    if (estado_libro_actualizar)
                    {
                        if (correo_usuario != null)
                        {
                            bool estado_prestamo = obj_prestamo_controlador.actualizarEstadoPrestamo(nombre_libro, correo_usuario, "Devuelto");
                            if (estado_prestamo)
                            {
                                MessageBox.Show("Prestamo ingresado exitosamente", "Tarea exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Prestamo ingresado exitosamente, pero no se ha logrado actualizar el estado del prestamo", "Tarea exitosa (Advertencia)", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            }
                        } else
                        {
                            // Que hacer en caso de que el campo usuario sea null
                        }

                    }
                    else
                    {
                        MessageBox.Show("Prestamo ingresado exitosamente, pero no se ha logrado actualizar el estado del libro", "Tarea exitosa (Advertencia)", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    ActualizarDataGrid();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error al guardar los datos {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void actualizarDevolucion()
        {
            try
            {
                if (index_tabla >= 0)
                {
                    bool actualizar = obj_controlador.actualizarDevolucion(index_tabla, cmbLibro.Text, cmbUsuario.Text, dtFechaDevolu.Text, lblMonto.Text, txtComentario.Text);
                    if (actualizar)
                    {
                        MessageBox.Show("Registro actualizado con exito", "Tarea exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ActualizarDataGrid();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"No se logro actualizar el registro {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void eliminarDevolucion()
        {
            try
            {
                if (MessageBox.Show("Deseas eliminar el prestamo ?", "Confirmacion de eliminacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (dgDevoluciones.SelectedRows.Count > 0)
                    {
                        if (index_tabla >= 0)
                        {
                            bool eliminar = obj_controlador.eliminarDevolucion(index_tabla);

                            if (eliminar)
                            {
                                MessageBox.Show("Devolucion eliminada exitosamente", "Tarea exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                ActualizarDataGrid();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error al eliminar los datos {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void validarFechaMontoDevolucion()
        {
            // Fecha de devolución real seleccionada por el usuario
            DateTime fechaDevolucionReal = dtFechaDevolu.Value;

            // Obtener la lista de préstamos desde el controlador
            var listaPrestamos = obj_prestamo_controlador.obtenerPrestamos();

            // Variables para controlar el mensaje
            bool devolucionEnPlazo = false;
            decimal montoPenalizacion = 0;
            string libroTitulo = string.Empty;

            if (listaPrestamos != null && listaPrestamos.Count > 0)
            {
                foreach (var elemento in listaPrestamos)
                {
                    // Simulación de obtener la fecha establecida para un préstamo específico
                    string fechaDevolucionEstablecidaStr = elemento.fecha_devolucion_estimada;

                    // Convertir la fecha de devolución establecida a DateTime
                    if (DateTime.TryParseExact(
                        fechaDevolucionEstablecidaStr,
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime fechaDevolucionEstablecida))
                    {
                        libroTitulo = elemento.titulo_libro;

                        // Validar si la devolución se hizo dentro del plazo
                        if (fechaDevolucionReal <= fechaDevolucionEstablecida)
                        {
                            devolucionEnPlazo = true;
                            lblMonto.Text = "$0.00"; // No hay penalización si está dentro del plazo
                        }
                        else
                        {
                            // Calcular la cantidad de meses de retraso
                            int mesesRetraso = ((fechaDevolucionReal.Year - fechaDevolucionEstablecida.Year) * 12) +
                                               fechaDevolucionReal.Month - fechaDevolucionEstablecida.Month;

                            if (fechaDevolucionReal.Day < fechaDevolucionEstablecida.Day)
                            {
                                mesesRetraso--; // Ajuste si el día actual es menor al día de la fecha establecida
                            }

                            // Penalización de $5 por cada mes de atraso
                            montoPenalizacion = (mesesRetraso * 5) + 5;
                            lblMonto.Text = $"${montoPenalizacion:0.00}";
                        }
                    }
                    else
                    {
                        MessageBox.Show("Error al convertir la fecha de devolución establecida para el préstamo.");
                        return; // Salimos del método si ocurre un error
                    }
                }

                // Mostrar un solo mensaje después de completar la iteración
                if (devolucionEnPlazo)
                {
                    MessageBox.Show($"Devolución realizada dentro del plazo para el libro: {libroTitulo}.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (montoPenalizacion > 0)
                {
                    MessageBox.Show($"La devolución para el libro: {libroTitulo} se realizó fuera del plazo. Penalización: {lblMonto.Text}.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("No se encontraron préstamos para validar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        void validarFechaMontoSinMensaje()
        {
            // Obtener la lista de préstamos desde el controlador
            var listaPrestamos = obj_prestamo_controlador.obtenerPrestamos();

            // Fecha de devolución real seleccionada por el usuario
            DateTime fechaDevolucionReal = dtFechaDevolu.Value;

            if (fechaDevolucionReal > DateTime.Now)
            {
                MessageBox.Show("La fecha de devolución no puede ser mayor a la fecha actual.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                dtFechaDevolu.Value = DateTime.Now;
            }

            if (listaPrestamos != null && listaPrestamos.Count > 0)
            {
                foreach (var elemento in listaPrestamos)
                {
                    // Simulación de obtener la fecha establecida para un préstamo específico
                    string fechaDevolucionEstablecidaStr = elemento.fecha_devolucion_estimada;

                    // Convertir la fecha de devolución establecida a DateTime
                    if (DateTime.TryParseExact(
                        fechaDevolucionEstablecidaStr,
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime fechaDevolucionEstablecida))
                    {
                        // Validar si la devolución se hizo dentro del plazo
                        if (fechaDevolucionReal <= fechaDevolucionEstablecida)
                        {
                            lblMonto.Text = "$0.00"; // No hay penalización si está dentro del plazo
                        }
                        else
                        {
                            // Calcular la cantidad de meses de retraso
                            int mesesRetraso = ((fechaDevolucionReal.Year - fechaDevolucionEstablecida.Year) * 12) +
                                               fechaDevolucionReal.Month - fechaDevolucionEstablecida.Month;

                            if (fechaDevolucionReal.Day < fechaDevolucionEstablecida.Day)
                            {
                                mesesRetraso--; // Ajuste si el día actual es menor al día de la fecha establecida
                            }

                            // Penalización de $5 por cada mes de atraso
                            decimal montoPenalizacion = (mesesRetraso * 5) + 5;
                            lblMonto.Text = $"${montoPenalizacion:0.00}";
                        }
                    }
                }
            }
        }


        private void dtFechaDevolu_ValueChanged_1(object sender, EventArgs e)
        {
            dtFechaDevolu.Format = DateTimePickerFormat.Short;
            validarFechaMontoSinMensaje();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            if (cmbLibro.SelectedIndex <= 0 && cmbUsuario.SelectedItem.ToString() == null && dtFechaDevolu.Value == null && string.IsNullOrWhiteSpace(lblMonto.Text) && string.IsNullOrWhiteSpace(txtComentario.Text))
            {
                MessageBox.Show("Todos los campos deben estar completos", "Campos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else
            {
                guardarDevolucion();
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            cargarCmbLibros();
            txtComentario.Clear();
            lblMonto.Text = " - ";
        }

        string buscarISBNPorNombreLibro(string p_nombreLibro)
        {
            var lista_libros = obj_libro_controlador.obtenerListaLibros();
            var libro = lista_libros.FirstOrDefault(l => l.titulo_libro == p_nombreLibro);

            return libro != null ? libro.ISBN : string.Empty;
        }

        private void dgDevoluciones_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgDevoluciones.Rows[e.RowIndex].Cells[0].Value != null)
            {
                index_tabla = e.RowIndex;

                var fila = dgDevoluciones.Rows[e.RowIndex].Cells;

                cmbLibro.Text = fila[0].Value?.ToString() ?? string.Empty;
                cmbUsuario.Text = fila[1].Value?.ToString() ?? string.Empty;
                dtFechaDevolu.Text = fila[2].Value?.ToString() ?? string.Empty;
                lblMonto.Text = fila[3].Value?.ToString() ?? string.Empty;
                txtComentario.Text = fila[4].Value?.ToString() ?? string.Empty;
            }
            else
            {
                index_tabla = -1;
            }
        }

        private void btnActualizar_Click(object sender, EventArgs e)
        {
            actualizarDevolucion();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            eliminarDevolucion();
        }
    }
}
