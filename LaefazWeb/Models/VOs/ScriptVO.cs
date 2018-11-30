using System.Collections.Generic;

namespace LaefazWeb.Models.VOs
{
    public class ScriptVO
    {
        public Script_CondicaoScript Script_CondicaoScript { get; set; }
        public Script Script { get; set; }
        public List<AmbienteExecucao> AmbienteExecucao { get; set; }
        public List<AmbienteVirtual> AmbienteVirtual { get; set; }
        public List<ParametroVO> ListaParametros { get ; set; }
        public List<ParametroVO> ListaParametrosEntrada { get; set; }
        public List<ParametroVO> ListaParametrosSaida { get; set; }
    }
}