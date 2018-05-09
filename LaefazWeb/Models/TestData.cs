//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LaefazWeb.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TestData
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TestData()
        {
            this.ParametroValor = new HashSet<ParametroValor>();
        }
    
        public int Id { get; set; }
        public Nullable<int> IdExecucao { get; set; }
        public int IdDataPool { get; set; }
        public int IdStatus { get; set; }
        public Nullable<int> IdUsuario { get; set; }
        public int IdScript_CondicaoScript { get; set; }
        public string Descricao { get; set; }
        public bool GerarMigracao { get; set; }
        public string CasoTesteRelativo { get; set; }
        public string Observacao { get; set; }
        public Nullable<System.DateTime> InicioExecucao { get; set; }
        public Nullable<System.DateTime> TerminoExecucao { get; set; }
        public string GeradoPor { get; set; }
        public Nullable<System.DateTime> DataGeracao { get; set; }
        public string CaminhoEvidencia { get; set; }
    
        public virtual DataPool DataPool { get; set; }
        public virtual Execucao Execucao { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ParametroValor> ParametroValor { get; set; }
        public virtual Script_CondicaoScript Script_CondicaoScript { get; set; }
        public virtual Status Status { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}
