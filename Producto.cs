// Zapateria

using System.Collections;
using System.Drawing;
using System.Reflection;
using static Npgsql.Replication.PgOutput.Messages.RelationMessage;
using static System.Net.Mime.MediaTypeNames;

namespace Calzado.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Modelo { get; set; } = "";
        public string Marca { get; set; } = "";
        // Nueva propiedad para variaciones de color
        public string Color { get; set; } = "";
        public decimal Talla { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        // Nueva propiedad para la foto del zapato
        public string ImagenUrl { get; set; } = "";
    }
}