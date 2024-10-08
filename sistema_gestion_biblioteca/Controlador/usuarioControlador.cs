﻿using sistema_gestion_biblioteca.Modelo;
using Newtonsoft.Json;
using System.Globalization;

namespace sistema_gestion_biblioteca.Controlador
{
    public class usuarioControlador
    {
        // Definimos las variables para encontrar el archivo json
        private string carpetaData;
        private string archivoJson;

        // Objeto para acceder a cualquier elemento de la clase
        public usuarioModelo obj_modelo = new usuarioModelo();

        // Constructor de la clase
        public usuarioControlador()
        {
            // Establecemos la carpeta root del proyecto
            string carpetaRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

            // Establecemos la ruta
            carpetaData = Path.Combine(carpetaRoot, "Data");

            // Creamos la carpeta Data si no existe
            if (!Directory.Exists(carpetaData))
            {
                Directory.CreateDirectory(carpetaData);
            }

            // Establecemos la ruta completa para el archivo .json
            archivoJson = Path.Combine(carpetaData, "usuarios.json");
        }

        // Metodo que me devuelve la lista desde el archivo json
        public List<usuarioModelo> obtenerUsuarios()
        {
            // Verificamos que el archivo JSON exista
            if (File.Exists(archivoJson))
            {
                string json = File.ReadAllText(archivoJson);
                var jsonConfig = new JsonSerializerSettings
                {
                    DateFormatString = "dd/MM/yyyy",
                    Culture = CultureInfo.InvariantCulture
                };
                return JsonConvert.DeserializeObject<List<usuarioModelo>>(json, jsonConfig) ?? new List<usuarioModelo>();
            }

            return new List<usuarioModelo>();
        }

        // Metodo con el cual agregamos un nuevo usuario a la lista
        public bool agregarUsuario(string p_nombres, string p_apellidos, string p_direccion, string p_telefono, string p_email, DateTime p_fecha_registro)
        {
            try
            {
                obj_modelo = new usuarioModelo
                {
                    nombres = p_nombres,
                    apellidos = p_apellidos,
                    direccion = p_direccion,
                    telefono = p_telefono,
                    email = p_email,
                    fechaRegistro = p_fecha_registro
                };
                var guardar = obtenerUsuarios();
                guardar.Add(obj_modelo); // Aqui agregamos los datos recogidos a la lista
                guardarUsuarios(guardar); // Aqui guardamos la lista dentro del JSON
                return true;
            } 
            catch
            {
                return false;
            }
        }

        // Metodo para actualizar el usuario y agregamos dicha lista modificada
        public bool actualizarUsuario(int index, string p_nombres, string p_apellidos, string p_direccion, string p_telefono, string p_email)
        {
            try
            {
                var actualizar = obtenerUsuarios();
                if (index < 0 || index >= actualizar.Count)
                {
                    return false;
                }
                else
                {
                    actualizar[index].nombres = p_nombres;
                    actualizar[index].apellidos = p_apellidos;
                    actualizar[index].direccion = p_direccion;
                    actualizar[index].telefono = p_telefono;
                    actualizar[index].email = p_email;
                    guardarUsuarios(actualizar); // Guardamos la nueva lista modificada y la agregamos directamente al archivo JSON
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Metodo para eliminar un usuario de la lista
        public bool eliminarUsuario(int index)
        {
            try
            {
                var eliminar = obtenerUsuarios();
                if (index < 0 || index >= eliminar.Count)
                {
                    return false;
                }
                else
                {
                    eliminar.RemoveAt(index);
                    guardarUsuarios(eliminar); // Agregamos la nueva lista ya modificada
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Metodo para Guardar los usuarios finalmente en el archivo JSON
        private void guardarUsuarios(List<usuarioModelo> p_usuarios)
        {
            var jsonConfig = new JsonSerializerSettings
            {
                DateFormatString = "dd/MM/yyyy"
            };

            var json = JsonConvert.SerializeObject(p_usuarios, Formatting.Indented, jsonConfig);
            File.WriteAllText(archivoJson, json);
        }


        // Metodo para validar que no existan usuarios duplicados por medio del email y el telefono
        public bool validarUsariosDuplicados(string p_email, string p_telefono)
        {
            var lista = obtenerUsuarios();
            return lista.Any(ele => ele.email.Equals(p_email, StringComparison.OrdinalIgnoreCase) || ele.telefono.Equals(p_telefono));
        }
    }
}
