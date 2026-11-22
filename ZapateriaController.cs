using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using Calzado.Models;

namespace Calzado.Controllers
{
    public class TiendaController : Controller
    {
        // PEGÁ ACÁ TU CADENA DE CONEXIÓN DE SESSION POOLER (con tu contraseña real)
        private readonly string _connectionString = "Host=aws-1-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.vvajlmvfnqhhkrtrfdvg;Password=proyecto123;SSL Mode=Require;Trust Server Certificate=true;";

        public IActionResult Index()
        {
            List<Producto> listaZapatos = new List<Producto>();

            using (var conexion = new NpgsqlConnection(_connectionString))
            {
                conexion.Open();
                string query = "SELECT id, modelo, marca, color, talla, precio, stock, imagen_url FROM productos WHERE stock > 0 ORDER BY id;";

                using (var comando = new NpgsqlCommand(query, conexion))
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        listaZapatos.Add(new Producto
                        {
                            Id = lector.GetInt32(0),
                            Modelo = lector.GetString(1),
                            Marca = lector.GetString(2),
                            Color = lector.GetString(3),
                            Talla = lector.GetDecimal(4),
                            Precio = lector.GetDecimal(5),
                            Stock = lector.GetInt32(6),
                            ImagenUrl = lector.GetString(7)
                        });
                    }
                }
            }

            return View(listaZapatos);
        }

        [HttpPost]
        public IActionResult IniciarSesion(string correo, string clave)
        {
            using (var conexion = new NpgsqlConnection(_connectionString))
            {
                conexion.Open();
                string sql = "SELECT id, nombre, identificacion, correo, rol, pais, direccion, tarjeta_numero FROM usuarios WHERE correo = @correo AND clave = @clave LIMIT 1;";

                using (var cmd = new NpgsqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@correo", correo ?? "");
                    cmd.Parameters.AddWithValue("@clave", clave ?? "");

                    using (var lector = cmd.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            var usuario = new Usuario
                            {
                                Id = lector.GetInt32(0),
                                Nombre = lector.GetString(1),
                                Identificacion = lector.GetString(2),
                                Correo = lector.GetString(3),
                                Rol = lector.GetString(4),
                                Pais = lector.GetString(5),
                                Direccion = lector.GetString(6),
                                TarjetaNumero = lector.GetString(7)
                            };
                            return Json(new { exito = true, datos = usuario });
                        }
                    }
                }
            }

            return Json(new { exito = false, mensaje = "Correo o contraseña incorrectos." });
        }

        [HttpPost]
        public IActionResult Comprar(int usuarioId, string productoIds, string clienteNombre, decimal precio, string metodoPago, string ultimosDigitos, string tipoEntrega, string pais, string direccion)
        {
            if (usuarioId <= 0)
            {
                return RedirectToAction("Index");
            }

            string detallePago = metodoPago;
            if (metodoPago == "Tarjeta" && !string.IsNullOrEmpty(ultimosDigitos) && ultimosDigitos.Length >= 4)
            {
                string cuatroDigitos = ultimosDigitos.Substring(ultimosDigitos.Length - 4);
                detallePago = $"Tarjeta (**** {cuatroDigitos})";
            }

            string direccionFinal = (tipoEntrega == "Envío a domicilio" && !string.IsNullOrEmpty(direccion))
                ? direccion
                : "Retiro en caja / tienda";

            using (var conexion = new NpgsqlConnection(_connectionString))
            {
                conexion.Open();

                string sqlVenta = "INSERT INTO ventas (cliente_nombre, total, metodo_pago, tipo_entrega, pais, direccion_envio) VALUES (@cliente, @total, @pago, @entrega, @pais, @dir);";
                using (var cmdVenta = new NpgsqlCommand(sqlVenta, conexion))
                {
                    cmdVenta.Parameters.AddWithValue("@cliente", clienteNombre);
                    cmdVenta.Parameters.AddWithValue("@total", precio);
                    cmdVenta.Parameters.AddWithValue("@pago", detallePago);
                    cmdVenta.Parameters.AddWithValue("@entrega", tipoEntrega);
                    cmdVenta.Parameters.AddWithValue("@pais", pais ?? "Nicaragua");
                    cmdVenta.Parameters.AddWithValue("@dir", direccionFinal);
                    cmdVenta.ExecuteNonQuery();
                }

                if (!string.IsNullOrEmpty(productoIds))
                {
                    string[] listaIds = productoIds.Split(',');
                    foreach (string idTexto in listaIds)
                    {
                        if (int.TryParse(idTexto, out int idZapato))
                        {
                            string sqlStock = "UPDATE productos SET stock = stock - 1 WHERE id = @id AND stock > 0;";
                            using (var cmdStock = new NpgsqlCommand(sqlStock, conexion))
                            {
                                cmdStock.Parameters.AddWithValue("@id", idZapato);
                                cmdStock.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}