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
    
    public partial class ParametroScript
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ParametroScript()
        {
            this.ParametroValor = new HashSet<ParametroValor>();
        }
    
        public int Id { get; set; }
        public int IdParametro { get; set; }
        public int IdScript_CondicaoScript { get; set; }
        public int IdTipoParametro { get; set; }
        public bool Obrigatorio { get; set; }
    
        public virtual Parametro Parametro { get; set; }
        public virtual Script_CondicaoScript Script_CondicaoScript { get; set; }
        public virtual TipoParametro TipoParametro { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ParametroValor> ParametroValor { get; set; }
    }
}
