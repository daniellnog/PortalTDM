using LaefazWeb.Models;
using System.Collections.Generic;

namespace LaefazWeb.Controllers
{
    public class ParametroValorVO
    {
        public string Valor { get; set; }
        public int Id { get; set; }
        public int IdParametroScript { get; set; }
        public int IdParametro { get; set; }
        public string Descricao { get; set; }
        public string Tipo { get; set; }
        public int IdTipoParametro { get; set; }
        public int IdStatusTestData { get; set; }

        public bool Obrigatorio { get; set; }
    }
}