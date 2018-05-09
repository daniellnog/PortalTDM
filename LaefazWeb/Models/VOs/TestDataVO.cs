using LaefazWeb.Models;
using System.Collections.Generic;

namespace LaefazWeb.Controllers
{
    public class TestDataVO
    {
      
        public string Descricao { get; set; }
        public string CasoTesteRelativo { get; set; }
        public string Observacao { get; set; }
        public System.DateTime? DataGeracao { get; set; }
        public string GeradoPor { get; set; }
        public string Status { get; set; }
        public List<ParametroValorVO> valores { get; set; }
        public int? IdTestData { get; set; }
        public int? IdParametroValor { get; set; }
        public int? IdStatus { get; set; }
        public int? IdScriptCondicaoScript { get; set; }
        public string migracaoCodifo { get; set; }

    }
}