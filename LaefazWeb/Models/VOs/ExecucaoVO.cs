using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaefazWeb.Models.VOs
{
    public class ExecucaoVO
    {
        public int IdExecucao { get; set; }
        public string DescricaoExecucao { get; set; }
        public string DescricaoTipoFaseTeste { get; set; }
        public int TotalCount { get; set; }
        public ExecucaoVO() { }

        public ExecucaoVO(ExecucaoVO execucao)
        {
            this.IdExecucao = execucao.IdExecucao;
            this.DescricaoExecucao = execucao.DescricaoExecucao;
            this.DescricaoTipoFaseTeste = execucao.DescricaoTipoFaseTeste;
        }
    }
}