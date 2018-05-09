using LaefazWeb.Models;
using System.Collections.Generic;

namespace LaefazWeb.Controllers
{
    public class ParametroVO
    {
        public string Descricao { get; set; }
        public int IdParametroScript { get; set; }
        public int IdParametro { get; set; }
        public int IdTipoParametro { get; set; }
        public string Tipo { get; set; }

    }
}