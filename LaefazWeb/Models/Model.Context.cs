﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DbEntities : DbContext
    {
        public DbEntities()
            : base("name=DbEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Agendamento> Agendamento { get; set; }
        public virtual DbSet<Agendamento_TestData> Agendamento_TestData { get; set; }
        public virtual DbSet<AmbienteExecucao> AmbienteExecucao { get; set; }
        public virtual DbSet<AmbienteVirtual> AmbienteVirtual { get; set; }
        public virtual DbSet<AUT> AUT { get; set; }
        public virtual DbSet<CondicaoScript> CondicaoScript { get; set; }
        public virtual DbSet<DataPool> DataPool { get; set; }
        public virtual DbSet<Demanda> Demanda { get; set; }
        public virtual DbSet<Encadeamento> Encadeamento { get; set; }
        public virtual DbSet<Encadeamento_TestData> Encadeamento_TestData { get; set; }
        public virtual DbSet<Execucao> Execucao { get; set; }
        public virtual DbSet<Historico> Historico { get; set; }
        public virtual DbSet<MapaCalor> MapaCalor { get; set; }
        public virtual DbSet<Parametro> Parametro { get; set; }
        public virtual DbSet<ParametroScript> ParametroScript { get; set; }
        public virtual DbSet<ParametroScript_Valor> ParametroScript_Valor { get; set; }
        public virtual DbSet<ParametroValor> ParametroValor { get; set; }
        public virtual DbSet<ParametroValor_Historico> ParametroValor_Historico { get; set; }
        public virtual DbSet<Script> Script { get; set; }
        public virtual DbSet<Script_CondicaoScript> Script_CondicaoScript { get; set; }
        public virtual DbSet<Script_CondicaoScript_Ambiente> Script_CondicaoScript_Ambiente { get; set; }
        public virtual DbSet<Status> Status { get; set; }
        public virtual DbSet<StatusExecucao> StatusExecucao { get; set; }
        public virtual DbSet<TDM> TDM { get; set; }
        public virtual DbSet<TDM_Usuario> TDM_Usuario { get; set; }
        public virtual DbSet<TelaMapaCalor> TelaMapaCalor { get; set; }
        public virtual DbSet<TestData> TestData { get; set; }
        public virtual DbSet<TipoDadoParametro> TipoDadoParametro { get; set; }
        public virtual DbSet<TipoFaseTeste> TipoFaseTeste { get; set; }
        public virtual DbSet<TipoParametro> TipoParametro { get; set; }
        public virtual DbSet<Usuario> Usuario { get; set; }
    }
}
