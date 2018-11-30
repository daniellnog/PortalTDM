using LaefazWeb.Enumerators;
using LaefazWeb.Extensions;
using LaefazWeb.Models;
using LaefazWeb.Models.VOs;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TDMWeb.Extensions;
using TDMWeb.Lib;
using WebGrease.Css.Extensions;

namespace LaefazWeb.Controllers
{
    [UsuarioLogado]
    public class ScriptController : Controller
    {
        //#if (DEBUG)
        //        public static string SqlConnectionString = @"Data Source=localhost;Database=TDM.Db;Trusted_Connection=true;Persist Security Info=True";
        //#else
        //        public static string SqlConnectionString = ConfigurationManager.ConnectionStrings["BulkInsert"].ConnectionString;
        //#endif


        private DbEntities db = new DbEntities();

        private static LogTDM log = new LogTDM(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public ActionResult Index()
        {
            return View(db.Script_CondicaoScript.ToList());
        }

        public ActionResult Adicionar(string id)
        {
            ViewBag.ListaAUT = db.AUT.ToList();
            ViewBag.ListaParametros = db.Parametro.ToList();
            ViewBag.ListaAmbientesExec = db.AmbienteExecucao.ToList();
            ViewBag.ListaAmbientesVirtual = db.AmbienteVirtual.ToList();
            ViewBag.TipoDadoParametro = db.TipoDadoParametro.ToList();
            ViewBag.ListaScriptPai = db.Script.ToList();
            ViewBag.CondicaoScript = db.CondicaoScript.ToList();

            ViewBag.ListaParametros = db.Parametro.ToList();

            return View();
        }

        public ActionResult Teste()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Remover(IList<string> ids)
        {
            var result = new JsonResult() { JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            for (int i = 0; i < ids.Count; i++)
            {
                Script_CondicaoScript scriptCS = null;
                Script script = null;
                try
                {
                    int idAtual = Int32.Parse(ids[i]);

                    int qtdTestDatas = (from td in db.TestData
                                        where td.IdScript_CondicaoScript == idAtual
                                        select td
                                       ).Count();

                    if (qtdTestDatas > 0)
                    {
                        result.Data = new { Result = "Não é possível excluir esse script pois o mesmo tem dependências com outras entidades.", Status = (int)WebExceptionStatus.SendFailure };
                    }
                    else
                    {
                        scriptCS = db.Script_CondicaoScript.Where(a => a.Id == idAtual).FirstOrDefault();


                        List<ParametroScript> ps = db.ParametroScript.Where(x => x.IdScript_CondicaoScript == scriptCS.Id).ToList();

                        List<Script_CondicaoScript_Ambiente> scsa = db.Script_CondicaoScript_Ambiente.Where(x => x.IdScript_CondicaoScript == scriptCS.Id).ToList();

                        for (int w = 0; w < scsa.Count; w++)
                        {
                            db.Script_CondicaoScript_Ambiente.Remove(scsa[w]);
                            log.Info("Script_CondicaoScript_Ambiente excluído com sucesso!");
                            log.Debug("Script_CondicaoScript_Ambiente: " + Util.ToString(scsa[w]));
                        }
                        db.SaveChanges();

                        for (int y = 0; y < ps.Count; y++)
                        {
                            db.ParametroScript.Remove(ps[y]);
                            log.Info("ParametroScript excluído com sucesso!");
                            log.Debug("ParametroScript: " + Util.ToString(ps[y]));
                        }
                        db.SaveChanges();

                        int qtdCondScripts = (from s in db.Script_CondicaoScript
                                              where s.IdScript == scriptCS.IdScript
                                              select s.Id
                                              ).Count();

                        script = db.Script.Where(x => x.Id == scriptCS.IdScript).FirstOrDefault();

                        db.Script_CondicaoScript.Remove(scriptCS);
                        db.SaveChanges();
                        log.Info("Script_CondicaoScript excluído com sucesso!");
                        log.Debug("Script_CondicaoScript: " + Util.ToString(scriptCS));

                        //caso seja == 1, quer dizer que o script deve ser excluído, pois é a última condição daquele script existente.
                        if (qtdCondScripts == 1)
                        {
                            db.Script.Remove(script);
                            db.SaveChanges();
                            log.Info("Script excluído com sucesso!");
                            log.Debug("Script: " + Util.ToString(script));
                            result.Data = new { Result = "Script removido com sucesso.", Status = (int)WebExceptionStatus.Success };
                        }
                        else
                        {
                            result.Data = new { Result = "Condição script removido com sucesso.", Status = (int)WebExceptionStatus.Success };
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.InnerException != null && (ex.InnerException.InnerException.Message.ToString().Contains("FK_TestData_DataPool") || ex.InnerException.InnerException.Message.ToString().Contains("FK_ParametroValor_ParametroScript") || ex.InnerException.InnerException.Message.ToString().Contains("FK_DataPool_Script_CondicaoScript")))
                    {
                        log.Error("Esse registro contém dependência com outra entidade. ", ex);
                        log.Debug("Script: " + Util.ToString(script));
                        result.Data = new { Result = "Esse registro contém dependência com outra entidade.", Status = (int)WebExceptionStatus.SendFailure };
                    }
                    else
                        result.Data = new { Result = ex.Message, Status = (int)WebExceptionStatus.UnknownError };

                    log.Error("Erro ao excluir o Script.", ex);
                    log.Debug("Script: " + Util.ToString(script));
                }
            }
            return result;
        }

        public ActionResult SalvarParametro(string descricao, string nome_tecnico, string tipo)
        {
            var result = new JsonResult() { JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            try
            {
                int idTipoParametro = Int32.Parse(tipo);
                TipoDadoParametro tdp = db.TipoDadoParametro.Where(x => x.Id == idTipoParametro).First();
                Parametro p = new Parametro()
                {
                    Descricao = descricao.ToUpper(),
                    ColunaTecnicaTosca = nome_tecnico.ToUpper(),
                    Tipo = tdp.Descricao
                };
                db.Parametro.Add(p);
                db.SaveChanges();
                result.Data = new { Result = "Parametro adicionado com sucesso.", Status = (int)WebExceptionStatus.Success, Id = p.Id };
            }
            catch (Exception ex)
            {
                result.Data = new { Result = ex.Message, Status = (int)WebExceptionStatus.UnknownError };
            }
            return result;
        }

        public ActionResult Editar(int id)
        {
            Script_CondicaoScript script_cs = new Script_CondicaoScript();
            script_cs = db.Script_CondicaoScript.Where(x => x.Id == id).FirstOrDefault();

            Script script = new Script();
            script = db.Script.FirstOrDefault(x => x.Id == script_cs.IdScript);

            ScriptVO svo = new ScriptVO()
            {
                Script_CondicaoScript = script_cs,
                Script = script,
                AmbienteExecucao = db.AmbienteExecucao.ToList(),
                AmbienteVirtual = db.AmbienteVirtual.ToList(),
                ListaParametrosEntrada = null,
                ListaParametrosSaida = null,
                ListaParametros = null
            };
            ViewBag.ListaScriptPai = db.Script.Where(x => x.Id != script.Id).ToList();
            ViewBag.ListaAUT = db.AUT.ToList();
            ViewBag.ListaParametros = db.Parametro.ToList();
            ViewBag.ListaAmbientesExec = db.AmbienteExecucao.ToList();
            ViewBag.ListaAmbientesVirtual = db.AmbienteVirtual.ToList();
            ViewBag.TipoDadoParametro = db.TipoDadoParametro.ToList();
            ViewBag.CondicaoScript = db.CondicaoScript.ToList();



            return View(svo);
        }

        public JsonResult GetParametros()
        {
            Entities db = new Entities();

            int TotalRows = 0;

            List<ParametroVO> parametros = (from p in db.Parametro
                                                //join ps in db.ParametroScript on p.Id equals ps.IdParametro
                                            select new ParametroVO
                                            {
                                                IdParametro = p.Id,
                                                Descricao = p.Descricao,
                                                //IdTipoParametro = ps.IdTipoParametro,
                                                //ValorParametroDefault = ps.ValorDefault,
                                                //VisivelEmTela = ps.VisivelEmTela,
                                                //Obrigatorio = ps.Obrigatorio,
                                            }).ToList();

            if (parametros.Any())
                TotalRows = parametros.Count();

            //string json = JsonConvert.SerializeObject(parametros, Formatting.Indented);

            //return Json(json, JsonRequestBehavior.AllowGet);

            // get Start (paging start index) and length (page size for paging)
            string draw = Request.Form.GetValues("draw").FirstOrDefault();
            string start = Request.Form.GetValues("start").FirstOrDefault();
            string length = Request.Form.GetValues("length").FirstOrDefault();
            //Get Sort columns value
            string sortColumn = Request.Form.GetValues("order[0][column]").FirstOrDefault();
            string sortColumnDir = Request.Form.GetValues("order[0][dir]").FirstOrDefault();
            string searchValue = Request.Form.GetValues("search[value]").FirstOrDefault();

            return Json(new { draw = draw, recordsFiltered = TotalRows, recordsTotal = TotalRows, data = parametros }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllParametros(int id)
        {
            Entities db = new Entities();

            List<ParametroVO> parametrosEntrada = (from p in db.Parametro
                                                   join ps in db.ParametroScript on p.Id equals ps.IdParametro
                                                   join scs in db.Script_CondicaoScript on ps.IdScript_CondicaoScript equals scs.Id
                                                   join s in db.Script on scs.IdScript equals s.Id
                                                   where scs.Id == id && ps.IdTipoParametro == (int)EnumTipoParametro.Input
                                                   select new ParametroVO
                                                   {
                                                       Descricao = p.Descricao,
                                                       ColunaTecnicaTosca = p.ColunaTecnicaTosca,
                                                       Obrigatorio = ps.Obrigatorio,
                                                       Tipo = p.Tipo,
                                                       ValorDefault = ps.ValorDefault,
                                                       IdTipoParametro = ps.IdTipoParametro,
                                                       IdParametro = ps.IdParametro,
                                                       VisivelEmTela = ps.VisivelEmTela
                                                   }
                                                   ).OrderBy(x => x.Descricao).ToList();

            List<ParametroVO> parametrosSaida = (from p in db.Parametro
                                                 join ps in db.ParametroScript on p.Id equals ps.IdParametro
                                                 join scs in db.Script_CondicaoScript on ps.IdScript_CondicaoScript equals scs.Id
                                                 join s in db.Script on scs.IdScript equals s.Id
                                                 where scs.Id == id && ps.IdTipoParametro == (int)EnumTipoParametro.Output
                                                 select new ParametroVO
                                                 {
                                                     Descricao = p.Descricao,
                                                     ColunaTecnicaTosca = p.ColunaTecnicaTosca,
                                                     Obrigatorio = ps.Obrigatorio,
                                                     ValorDefault = ps.ValorDefault,
                                                     Tipo = p.Tipo,
                                                     IdTipoParametro = ps.IdTipoParametro,
                                                     IdParametro = ps.IdParametro,
                                                     VisivelEmTela = ps.VisivelEmTela
                                                 }
                                                   ).OrderBy(x => x.Descricao).ToList();

            int[] IdsEntrada = new int[parametrosEntrada.Count];
            int[] IdsSaida = new int[parametrosSaida.Count];

            for (int x = 0; x < parametrosEntrada.Count; x++)
            {
                IdsEntrada[x] = parametrosEntrada[x].IdParametro;
            }

            for (int y = 0; y < parametrosSaida.Count; y++)
            {
                IdsSaida[y] = parametrosSaida[y].IdParametro;
            }

            List<ParametroVO> parametros = (from p in db.Parametro
                                            where !IdsEntrada.Contains(p.Id) && !IdsSaida.Contains(p.Id)
                                            select new ParametroVO
                                            {
                                                IdParametro = p.Id,
                                                Descricao = p.Descricao,
                                                ColunaTecnicaTosca = p.ColunaTecnicaTosca,
                                                Tipo = p.Tipo
                                            }).OrderBy(x => x.Descricao).ToList();

            List<AmbienteVirtualVO> amb_virt = (from scsa in db.Script_CondicaoScript_Ambiente
                                                join av in db.AmbienteVirtual on scsa.IdAmbienteVirtual equals av.Id
                                                join scs in db.Script_CondicaoScript on scsa.IdScript_CondicaoScript equals scs.Id
                                                where scs.Id == id
                                                select new AmbienteVirtualVO
                                                {
                                                    Id = scsa.IdAmbienteVirtual,
                                                    Descricao = av.Descricao
                                                }).DistinctBy(x => x.Descricao).ToList();

            List<AmbienteExecucaoVO> amb_exec = (from scsa in db.Script_CondicaoScript_Ambiente
                                                 join av in db.AmbienteExecucao on scsa.IdAmbienteExecucao equals av.Id
                                                 join scs in db.Script_CondicaoScript on scsa.IdScript_CondicaoScript equals scs.Id
                                                 where scs.Id == id
                                                 select new AmbienteExecucaoVO
                                                 {
                                                     Id = scsa.IdAmbienteExecucao,
                                                     Descricao = av.Descricao
                                                 }).DistinctBy(x => x.Descricao).ToList();

            int?[] arrExec = new int?[amb_exec.Count];

            for (int x = 0; x < amb_exec.Count; x++)
            {
                arrExec[x] = amb_exec[x].Id;
            }
            int?[] arrVirt = new int?[amb_virt.Count];

            for (int y = 0; y < amb_virt.Count; y++)
            {
                arrVirt[y] = amb_virt[y].Id;
            }

            List<AmbienteExecucaoVO> ambientes_exec = (from amb in db.AmbienteExecucao
                                                       where !arrExec.Contains(amb.Id)
                                                       select new AmbienteExecucaoVO
                                                       {
                                                           Id = amb.Id,
                                                           Descricao = amb.Descricao
                                                       }).ToList();

            List<AmbienteVirtualVO> ambientes_virt = (from amb in db.AmbienteVirtual
                                                      where !arrVirt.Contains(amb.Id)
                                                      select new AmbienteVirtualVO
                                                      {
                                                          Id = amb.Id,
                                                          Descricao = amb.Descricao
                                                      }).ToList();


            List<Object> parametrosFinais = new List<Object>();
            parametrosFinais.Add(parametros);
            parametrosFinais.Add(parametrosEntrada);
            parametrosFinais.Add(parametrosSaida);
            parametrosFinais.Add(amb_exec);
            parametrosFinais.Add(amb_virt);
            parametrosFinais.Add(ambientes_exec);
            parametrosFinais.Add(ambientes_virt);

            string json = JsonConvert.SerializeObject(parametrosFinais, Formatting.Indented);

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SalvarEdicao(ScriptVO objeto)
        {
            try
            {
                Salvar(objeto, true);

                //this.FlashSuccess("Datapool editado com sucesso.");
            }
            catch (Exception ex)
            {
                this.FlashError(ex.Message);
                //throw new Exception(ex.Message);
            }
            return RedirectToAction("Index");
        }

        public string MakeQueryTosca()
        {
            string sb = "SELECT DISTINCT top 10000 tdt.Id AS ID_TEST_DATA, scr.Descricao AS SCRIPT, dem.Descricao AS DEMANDA, tdt.CasoTesteRelativo AS CASO_TESTE, aut.Descricao AS SISTEMA, ambexec.descricao as 'AMBIENTE EXECUCAO', ";

            string parametrosEntrada = Request.Form.Get("ParametrosEntrada");
            //Request["arrayParametrosEntrada"].ToString();
            string parametrosSaida = Request.Form.Get("ParametrosSaida");
            //Request["arrayParametrosSaida"].ToString();

            var listaEntradaString = parametrosEntrada.Split('|');
            var listaSaidaString = parametrosSaida.Split('|');
            List<ParametroVO> listaParametrosEntrada = new List<ParametroVO>();
            List<ParametroVO> listaParametrosSaida = new List<ParametroVO>();
            if (parametrosEntrada != "")
            {
                for (int i = 0; i < listaEntradaString.Length; i++)
                {
                    var obj = listaEntradaString[i].Split(',');

                    if (listaEntradaString[i] != "")
                    {
                        bool obrigatorio;
                        bool visivelEmTela;

                        if (obj[4] == "0")
                        {
                            obrigatorio = false;
                        }
                        else
                        {
                            obrigatorio = true;
                        }
                        if (obj[5] == "0")
                        {
                            visivelEmTela = false;
                        }
                        else
                        {
                            visivelEmTela = true;
                        }
                        ParametroVO par = new ParametroVO(Int32.Parse(obj[0]), obj[1], obj[2], obj[3], obrigatorio, visivelEmTela);

                        listaParametrosEntrada.Add(par);

                        string nomeParametro = listaParametrosEntrada[i].Descricao;
                        int idParam = Int32.Parse(obj[0]);
                        Parametro p = db.Parametro.Where(x => x.Id == idParam).FirstOrDefault();
                        string nomeTecnico = p.ColunaTecnicaTosca;

                        //listaParametrosEntrada[i].ColunaTecnicaTosca;
                        sb += "(SELECT parvAux.valor FROM ParametroValor parvAux INNER JOIN ParametroScript parsAux ON parvAux.IdParametroScript = parsAux.Id INNER JOIN Parametro parAux ON parsAux.IdParametro = parAux.Id WHERE parAux.Descricao = '" + nomeParametro + "' AND parvAux.IdTestData = tdt.Id) AS " + nomeTecnico;
                        if (parametrosSaida == "")
                        {
                            if (i != listaEntradaString.Length - 1)
                            {
                                sb += ",";
                            }
                        }
                        else
                        {
                            sb += ",";
                        }
                    }
                }
            }

            if (parametrosSaida != "")
            {
                for (int y = 0; y < listaSaidaString.Length - 1; y++)
                {
                    var obj = listaSaidaString[y].Split(',');

                    if (listaSaidaString[y] != "")
                    {
                        bool obrigatorio;
                        bool visivelEmTela;

                        if (obj[4] == "0")
                        {
                            obrigatorio = false;
                        }
                        else
                        {
                            obrigatorio = true;
                        }
                        if (obj[5] == "0")
                        {
                            visivelEmTela = false;
                        }
                        else
                        {
                            visivelEmTela = true;
                        }
                        ParametroVO parSaida = new ParametroVO(Int32.Parse(obj[0]), obj[1], obj[2], obj[3], obrigatorio, visivelEmTela);

                        listaParametrosSaida.Add(parSaida);

                        string nomeParam = listaParametrosSaida[y].Descricao;
                        int idParam = Int32.Parse(obj[0]);
                        Parametro p = db.Parametro.Where(x => x.Id == idParam).FirstOrDefault();
                        string nomeTec = p.ColunaTecnicaTosca;

                        //listaParametrosEntrada[i].ColunaTecnicaTosca;
                        sb += "(SELECT parvAux.valor FROM ParametroValor parvAux INNER JOIN ParametroScript parsAux ON parvAux.IdParametroScript = parsAux.Id INNER JOIN Parametro parAux ON parsAux.IdParametro = parAux.Id WHERE parAux.Descricao = '" + nomeParam + "' AND parvAux.IdTestData = tdt.Id) AS " + nomeTec;

                        if (y != listaSaidaString.Length - 2)
                        {
                            sb += ",";
                        }
                    }
                }
            }
            sb += " FROM";
            sb = sb.Replace(", FROM", " FROM");
            //sb.Substring(0, sb.Length - 1);

            sb += " TestData tdt INNER JOIN DataPool dtp ON tdt.IdDataPool = dtp.Id LEFT JOIN Script_CondicaoScript scrcon ON dtp.IdScript_CondicaoScript = scrcon.Id LEFT JOIN Script_CondicaoScript_Ambiente scrconamb ON scrconamb.IdScript_CondicaoScript = scrcon.Id INNER JOIN Script scr ON scrcon.IdScript = scr.Id LEFT JOIN CondicaoScript con ON scrcon.IdCondicaoScript = con.Id INNER JOIN ParametroScript parscr ON parscr.IdScript_CondicaoScript = scrcon.Id INNER JOIN Parametro par ON parscr.IdParametro = par.Id INNER JOIN AUT aut ON dtp.IdAut = aut.Id LEFT JOIN Execucao execu ON execu.IdTestData = tdt.Id INNER JOIN AmbienteExecucao ambexec ON scrconamb.IdAmbienteExecucao = ambexec.Id AND execu.IdScript_CondicaoScript_Ambiente = scrconamb.Id INNER JOIN AmbienteVirtual ambvir ON scrconamb.IdAmbienteVirtual = ambvir.Id LEFT JOIN Demanda dem ON dtp.IdDemanda = dem.Id INNER JOIN Usuario us ON tdt.IdUsuario = us.Id WHERE tdt.Id IN (ptdTosca) ORDER BY tdt.id";
            return sb;
        }

        public List<ParametroVO> getListaParametrosEntrada()
        {
            string parametrosEntrada = Request.Form.Get("ParametrosEntrada");

            var listaStringEntrada = parametrosEntrada.Split('|');

            List<ParametroVO> listaParametrosEntrada = new List<ParametroVO>();

            if (parametrosEntrada != "")
            {
                for (int i = 0; i < listaStringEntrada.Length; i++)
                {
                    var obj = listaStringEntrada[i].Split(',');

                    if (listaStringEntrada[i] != "")
                    {
                        bool obrigatorio = true;
                        bool visivelEmTela = true;

                        if (obj.Count() % 6 == 0)
                        {
                            if (obj[4] == "0")
                            {
                                obrigatorio = false;
                            }
                            else
                            {
                                obrigatorio = true;
                            }
                            if (obj[5] == "0")
                            {
                                visivelEmTela = false;
                            }
                            else
                            {
                                visivelEmTela = true;
                            }
                        }
                        else if (obj.Count() % 8 == 0)
                        {
                            if (obj[6] == "0")
                            {
                                obrigatorio = false;
                            }
                            else
                            {
                                obrigatorio = true;
                            }
                            if (obj[7] == "0")
                            {
                                visivelEmTela = false;
                            }
                            else
                            {
                                visivelEmTela = true;
                            }
                        }

                        ParametroVO par = new ParametroVO(Int32.Parse(obj[0]), obj[1], obj[2], obj[3], obrigatorio, visivelEmTela);

                        listaParametrosEntrada.Add(par);
                    }
                }
            }
            return listaParametrosEntrada;
        }

        public List<ParametroVO> getListaParametrosSaida()
        {
            string parametrosSaida = Request.Form.Get("ParametrosSaida");

            var listaStringSaida = parametrosSaida.Split('|');

            List<ParametroVO> listaParametrosSaida = new List<ParametroVO>();

            if (parametrosSaida != "")
            {
                for (int i = 0; i < listaStringSaida.Length; i++)
                {
                    var obj = listaStringSaida[i].Split(',');

                    if (listaStringSaida[i] != "")
                    {
                        bool obrigatorio = true;
                        bool visivelEmTela = true;

                        if (obj.Count() % 6 == 0)
                        {
                            if (obj[4] == "0")
                            {
                                obrigatorio = false;
                            }
                            else
                            {
                                obrigatorio = true;
                            }
                            if (obj[5] == "0")
                            {
                                visivelEmTela = false;
                            }
                            else
                            {
                                visivelEmTela = true;
                            }
                        }
                        else if (obj.Count() % 8 == 0)
                        {
                            if (obj[6] == "0")
                            {
                                obrigatorio = false;
                            }
                            else
                            {
                                obrigatorio = true;
                            }
                            if (obj[7] == "0")
                            {
                                visivelEmTela = false;
                            }
                            else
                            {
                                visivelEmTela = true;
                            }
                        }
                        ParametroVO par = new ParametroVO(Int32.Parse(obj[0]), obj[1], obj[2], obj[3], obrigatorio, visivelEmTela);

                        listaParametrosSaida.Add(par);
                    }
                }
            }

            return listaParametrosSaida;
        }

        public void inserirAmbientes(int idScript_CondicaoScript, bool editar = false)
        {
            string AmbienteVirtual = Request.Form.Get("listambientesvirtual");
            string AmbienteExec = Request.Form.Get("listambientesexec");

            string[] listaAmbientesVirtuais = AmbienteVirtual.Split(',');
            string[] listaAmbientesExec = AmbienteExec.Split(',');

            if (editar)
            {
                List<Script_CondicaoScript_Ambiente> ambientesBanco = (from s in db.Script_CondicaoScript_Ambiente
                                                                       where s.IdScript_CondicaoScript == idScript_CondicaoScript
                                                                       select s
                                                                       ).ToList();
                List<Script_CondicaoScript_Ambiente> ambienteTela = new List<Script_CondicaoScript_Ambiente>();
                string listaParesAmbBanco = "";
                string listaParesAmbTela = "";
                if (ambientesBanco.Count > 0)
                {
                    for (int i = 0; i < ambientesBanco.Count; i++)
                    {

                        listaParesAmbBanco += ambientesBanco[i].IdAmbienteExecucao + "|" + ambientesBanco[i].IdAmbienteVirtual;

                        if (i != ambientesBanco.Count - 1)
                            listaParesAmbBanco += ",";
                    }
                }

                var listaParesBancoString = listaParesAmbBanco.Split(',');

                for (int i = 0; i < listaAmbientesVirtuais.Length; i++)
                {
                    for (int w = 0; w < listaAmbientesExec.Length; w++)
                    {
                        string descAmbExec = listaAmbientesExec[w];
                        string descAmbVirt = listaAmbientesVirtuais[i];
                        int idAmbExec = db.AmbienteExecucao.Where(x => x.Descricao == descAmbExec).First().Id;
                        int idAmbVirt = db.AmbienteVirtual.Where(x => x.Descricao == descAmbVirt).First().Id;

                        listaParesAmbTela += idAmbExec + "|" + idAmbVirt + ",";
                    }
                }
                listaParesAmbTela = listaParesAmbTela.Remove(listaParesAmbTela.Length - 1);
                var listaParesTelaString = listaParesAmbTela.Split(',');

                for (int i = 0; i < listaParesTelaString.Length; i++)
                {
                    if (!listaParesBancoString.Contains(listaParesTelaString[i]))
                    {
                        int idAmbExec = Int32.Parse(listaParesTelaString[i].Substring(0, 1));
                        int idAmbVirt = Int32.Parse(listaParesTelaString[i].Substring(2, 1));
                        Script_CondicaoScript_Ambiente scsa = new Script_CondicaoScript_Ambiente()
                        {
                            IdScript_CondicaoScript = idScript_CondicaoScript,
                            IdAmbienteExecucao = idAmbExec,
                            IdAmbienteVirtual = idAmbVirt
                        };
                        db.Script_CondicaoScript_Ambiente.Add(scsa);
                        log.Info("Script_CondicaoScript_Ambiente adicionado com sucesso!");
                        log.Debug("Script_CondicaoScript_Ambiente: " + Util.ToString(scsa));
                    }

                }
                db.SaveChanges();
                if (listaParesBancoString.Length > 0)
                {
                    for (int i = 0; i < listaParesBancoString.Length; i++)
                    {
                        if (!listaParesTelaString.Contains(listaParesBancoString[i]))
                        {
                            int idAmbExec = Int32.Parse(listaParesBancoString[i].Substring(0, 1));
                            int idAmbVirt = Int32.Parse(listaParesBancoString[i].Substring(2, 1));
                            Script_CondicaoScript_Ambiente scsa = db.Script_CondicaoScript_Ambiente.Where(x => x.IdAmbienteExecucao == idAmbExec && x.IdAmbienteVirtual == idAmbVirt && x.IdScript_CondicaoScript == idScript_CondicaoScript).First();
                            db.Script_CondicaoScript_Ambiente.Remove(scsa);
                            log.Info("Script_CondicaoScript_Ambiente removido com sucesso!");
                            log.Debug("Script_CondicaoScript_Ambiente: " + Util.ToString(scsa));
                        }
                    }
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                        this.FlashError("" + e);
                        if (e.InnerException != null && e.InnerException.InnerException.Message.ToString().Contains("FK_Script_CondicaoScript_Ambiente_Execucao"))
                        {
                            log.Warn("Não é possível excluir esses ambientes, pois o mesmo já possui execuções nesses ambientes.");
                            this.FlashError("Não é possível excluir esses ambientes, pois o mesmo já possui execuções nesses ambientes.");
                        }
                        else
                        {
                            log.Warn("Não foi possível alterar os ambientes.");
                            this.FlashError(e.Message);
                        }
                    }
                }
            }
            else
            {
                for (int w = 0; w < listaAmbientesVirtuais.Length; w++)
                {
                    for (int y = 0; y < listaAmbientesExec.Length; y++)
                    {
                        Script_CondicaoScript_Ambiente scsa = new Script_CondicaoScript_Ambiente()
                        {
                            IdScript_CondicaoScript = idScript_CondicaoScript,
                            IdAmbienteVirtual = Int32.Parse(listaAmbientesVirtuais[w]),
                            IdAmbienteExecucao = Int32.Parse(listaAmbientesExec[y])
                        };
                        db.Script_CondicaoScript_Ambiente.Add(scsa);
                        log.Info("Ambiente adicionado com sucesso na tabela Script_CondicaoScript_Ambiente");
                        log.Debug("Script_CondicaoScript_Ambiente = " + Util.ToString(scsa));
                    }
                }
                db.SaveChanges();
            }
        }

        public List<ParametroVO> getParametrosEntradaBanco(int IdScript_CondicaoScript)
        {
            List<ParametroVO> ParametrosEntrada = (from ps in db.ParametroScript
                                                   join p in db.Parametro on ps.IdParametro equals p.Id
                                                   where ps.IdScript_CondicaoScript == IdScript_CondicaoScript && ps.IdTipoParametro == (int)EnumTipoParametro.Input
                                                   select new ParametroVO
                                                   {
                                                       IdParametro = ps.Id,
                                                       IdParametroScript = ps.IdParametro,
                                                       IdTipoParametro = ps.IdTipoParametro,
                                                       Obrigatorio = ps.Obrigatorio,
                                                       ColunaTecnicaTosca = p.ColunaTecnicaTosca,
                                                       IdScript_CondicaoScript = ps.IdScript_CondicaoScript,
                                                       VisivelEmTela = ps.VisivelEmTela,
                                                       ValorDefault = ps.ValorDefault
                                                   }
                                                 ).ToList();
            return ParametrosEntrada;
        }

        public List<ParametroVO> getParametrosSaidaBanco(int IdScript_CondicaoScript)
        {
            List<ParametroVO> ParametrosSaida = (from ps in db.ParametroScript
                                                 join p in db.Parametro on ps.IdParametro equals p.Id
                                                 where ps.IdScript_CondicaoScript == IdScript_CondicaoScript && ps.IdTipoParametro == (int)EnumTipoParametro.Output
                                                 select new ParametroVO
                                                 {
                                                     IdParametro = ps.Id,
                                                     IdParametroScript = ps.IdParametro,
                                                     IdTipoParametro = ps.IdTipoParametro,
                                                     Obrigatorio = ps.Obrigatorio,
                                                     ColunaTecnicaTosca = p.ColunaTecnicaTosca,
                                                     IdScript_CondicaoScript = ps.IdScript_CondicaoScript,
                                                     VisivelEmTela = ps.VisivelEmTela,
                                                     ValorDefault = ps.ValorDefault
                                                 }
                                                 ).ToList();

            return ParametrosSaida;
        }

        public bool inserirParametros(List<ParametroVO> listaParametrosEntrada, List<ParametroVO> listaParametrosSaida, int idScript, int IdScript_CondicaoScript, bool editar = false)
        {
            bool retorno;
            try
            {
                if (editar)
                {
                    List<ParametroVO> paramEntradaBanco = getParametrosEntradaBanco(IdScript_CondicaoScript);
                    List<ParametroVO> paramSaidaBanco = getParametrosSaidaBanco(IdScript_CondicaoScript);

                    List<ParametroVO> paramEntradaTela = listaParametrosEntrada;
                    List<ParametroVO> paramSaidaTela = listaParametrosSaida;

                    string listaParametrosEntradaBanco = "";
                    string listaParametrosSaidaBanco = "";
                    string listaParametrosEntradaTela = "";
                    string listaParametrosSaidaTela = "";

                    //crio uma string com todos os Id's, obrigatoriedades e visiveis em tela dos parametros de entrada que vieram do banco de dados.
                    if (paramEntradaBanco.Count > 0)
                    {
                        for (int i = 0; i < paramEntradaBanco.Count; i++)
                        {
                            listaParametrosEntradaBanco += paramEntradaBanco[i].IdParametroScript + "," + paramEntradaBanco[i].Obrigatorio + "," + paramEntradaBanco[i].VisivelEmTela + "," + paramEntradaBanco[i].ValorDefault + "|";
                        }
                        listaParametrosEntradaBanco = listaParametrosEntradaBanco.Remove(listaParametrosEntradaBanco.Length - 1);
                    }
                    //crio uma string com todos os Id's, obrigatoriedades e visiveis em tela dos parametros de saída que vieram do banco de dados.
                    if (paramSaidaBanco.Count > 0)
                    {
                        for (int i = 0; i < paramSaidaBanco.Count; i++)
                        {
                            listaParametrosSaidaBanco += paramSaidaBanco[i].IdParametroScript + "," + paramSaidaBanco[i].Obrigatorio + "," + paramSaidaBanco[i].VisivelEmTela + "," + paramSaidaBanco[i].ValorDefault + "|";
                        }
                        listaParametrosSaidaBanco = listaParametrosSaidaBanco.Remove(listaParametrosSaidaBanco.Length - 1);
                    }
                    //crio uma string com todos os Id's, obrigatoriedades e visiveis em tela dos parametros de entrada que vieram da tela.
                    if (paramEntradaTela.Count > 0)
                    {
                        for (int x = 0; x < paramEntradaTela.Count; x++)
                        {
                            listaParametrosEntradaTela += paramEntradaTela[x].IdParametro + "," + paramEntradaTela[x].Obrigatorio + "," + paramEntradaTela[x].VisivelEmTela + "," + paramEntradaTela[x].ValorDefault + "|";
                        }
                        listaParametrosEntradaTela = listaParametrosEntradaTela.Remove(listaParametrosEntradaTela.Length - 1);
                    }
                    //crio uma string com todos os Id's, obrigatoriedades e visiveis em tela dos parametros de saída que vieram da tela.
                    if (paramSaidaTela.Count > 0)
                    {
                        for (int y = 0; y < paramSaidaTela.Count; y++)
                        {
                            listaParametrosSaidaTela += paramSaidaTela[y].IdParametro + "," + paramSaidaTela[y].Obrigatorio + "," + paramSaidaTela[y].VisivelEmTela + "," + paramSaidaTela[y].ValorDefault + "|";
                        }
                        listaParametrosSaidaTela = listaParametrosSaidaTela.Remove(listaParametrosSaidaTela.Length - 1);
                    }
                    //Converto todas as strings em arrays
                    string[] arrParamEntradaTela = listaParametrosEntradaTela.Split('|');
                    string[] arrParamSaidaTela = listaParametrosSaidaTela.Split('|');
                    string[] arrParamEntradaBanco = listaParametrosEntradaBanco.Split('|');
                    string[] arrParamSaidaBanco = listaParametrosSaidaBanco.Split('|');

                    if (arrParamEntradaBanco[0] != "")
                    {
                        for (int i = 0; i < arrParamEntradaBanco.Length; i++)
                        {
                            string[] arrTempParamEntBanco = arrParamEntradaBanco[i].Split(',');
                            int idParamBanco = Int32.Parse(arrTempParamEntBanco[0]);
                            int tipoParamBanco = (int)EnumTipoParametro.Input;
                            // Caso tenha parâmetros de entrada na tela
                            #region
                            if (arrParamEntradaTela[0] != "")
                            {
                                bool temp = false;
                                for (int x = 0; x < arrParamEntradaTela.Length; x++)
                                {
                                    string[] arrTempParamEntTela = arrParamEntradaTela[x].Split(',');
                                    if (arrTempParamEntTela[0] == arrTempParamEntBanco[0])
                                    {
                                        temp = true;
                                        int idParam = Int32.Parse(arrTempParamEntTela[0]);
                                        int tipoParam = (int)EnumTipoParametro.Input;
                                        ParametroScript ps = db.ParametroScript.Where(y => y.IdParametro == idParam).Where(y => y.IdScript_CondicaoScript == IdScript_CondicaoScript).Where(y => y.IdTipoParametro == tipoParam).First();
                                        if (arrTempParamEntTela[1] != arrTempParamEntBanco[1])
                                        {
                                            ps.Obrigatorio = Convert.ToBoolean(arrTempParamEntTela[1]);
                                            db.ParametroScript.Attach(ps);
                                            //Prepara a entidade para uma Edição
                                            db.Entry(ps).State = System.Data.Entity.EntityState.Modified;
                                        }
                                        if (arrTempParamEntTela[2] != arrTempParamEntBanco[2])
                                        {
                                            ps.VisivelEmTela = Convert.ToBoolean(arrTempParamEntTela[2]);
                                            db.ParametroScript.Attach(ps);
                                            //Prepara a entidade para uma Edição
                                            db.Entry(ps).State = System.Data.Entity.EntityState.Modified;
                                        }
                                        if (arrTempParamEntTela[3] != arrTempParamEntBanco[3])
                                        {
                                            ps.ValorDefault = arrTempParamEntTela[3];
                                            db.ParametroScript.Attach(ps);
                                            //Prepara a entidade para uma Edição
                                            db.Entry(ps).State = System.Data.Entity.EntityState.Modified;
                                        }
                                    }
                                }
                                if (temp == false)
                                {
                                    ParametroScript ps = db.ParametroScript.Where(y => y.IdParametro == idParamBanco).Where(y => y.IdScript_CondicaoScript == IdScript_CondicaoScript).Where(y => y.IdTipoParametro == tipoParamBanco).First();
                                    db.ParametroScript.Remove(ps);
                                }
                            }
                            #endregion
                            //Caso não tenha parâmetros de entrada na tela
                            #region
                            else
                            {
                                int idParam = Int32.Parse(arrTempParamEntBanco[i]);
                                int tipoParam = (int)EnumTipoParametro.Input;
                                ParametroScript ps = db.ParametroScript.Where(y => y.IdParametro == idParam).Where(y => y.IdScript_CondicaoScript == IdScript_CondicaoScript).Where(y => y.IdTipoParametro == tipoParam).First();
                                db.ParametroScript.Remove(ps);
                                //db.SaveChanges();
                            }
                            #endregion
                        }
                    }
                    //db.SaveChanges();
                    if (arrParamSaidaBanco[0] != "")
                    {
                        for (int i = 0; i < arrParamSaidaBanco.Length; i++)
                        {
                            string[] arrTempParamSaiBanco = arrParamSaidaBanco[i].Split(',');
                            bool temp = false;
                            // Caso tenha parâmetros de saida na tela
                            #region
                            if (arrParamSaidaTela[0] != "")
                            {

                                for (int x = 0; x < arrParamSaidaTela.Length; x++)
                                {
                                    string[] arrTempParamSaiTela = arrParamSaidaTela[x].Split(',');
                                    if (arrTempParamSaiTela[0] == arrTempParamSaiBanco[0])
                                    {
                                        temp = true;
                                        int idParam = Int32.Parse(arrTempParamSaiTela[0]);
                                        int tipoParam = (int)EnumTipoParametro.Output;
                                        ParametroScript ps = db.ParametroScript.Where(y => y.IdParametro == idParam).Where(y => y.IdScript_CondicaoScript == IdScript_CondicaoScript).Where(y => y.IdTipoParametro == tipoParam).First();
                                        if (arrTempParamSaiTela[1] != arrTempParamSaiBanco[1])
                                        {
                                            ps.Obrigatorio = Convert.ToBoolean(arrTempParamSaiTela[1]);
                                            db.ParametroScript.Attach(ps);
                                            //Prepara a entidade para uma Edição
                                            db.Entry(ps).State = System.Data.Entity.EntityState.Modified;
                                        }
                                        if (arrTempParamSaiTela[2] != arrTempParamSaiBanco[2])
                                        {
                                            ps.VisivelEmTela = Convert.ToBoolean(arrTempParamSaiTela[2]);
                                            db.ParametroScript.Attach(ps);
                                            //Prepara a entidade para uma Edição
                                            db.Entry(ps).State = System.Data.Entity.EntityState.Modified;
                                        }
                                        if (arrTempParamSaiTela[3] != arrTempParamSaiBanco[3])
                                        {
                                            ps.ValorDefault = arrTempParamSaiTela[3];
                                            db.ParametroScript.Attach(ps);
                                            //Prepara a entidade para uma Edição
                                            db.Entry(ps).State = System.Data.Entity.EntityState.Modified;
                                        }
                                    }
                                }
                            }
                            #region
                            else
                            {
                                int idParam = Int32.Parse(arrTempParamSaiBanco[i]);
                                int tipoParam = (int)EnumTipoParametro.Output;
                                ParametroScript ps = db.ParametroScript.Where(y => y.IdParametro == idParam).Where(y => y.IdScript_CondicaoScript == IdScript_CondicaoScript).Where(y => y.IdTipoParametro == tipoParam).First();
                                db.ParametroScript.Remove(ps);
                                //db.SaveChanges();
                            }
                            #endregion
                            if (temp == false)
                            {
                                int idParam = Int32.Parse(arrTempParamSaiBanco[0]);
                                int tipoParam = (int)EnumTipoParametro.Output;
                                ParametroScript ps = db.ParametroScript.Where(y => y.IdParametro == idParam).Where(y => y.IdScript_CondicaoScript == IdScript_CondicaoScript).Where(y => y.IdTipoParametro == tipoParam).First();
                                db.ParametroScript.Remove(ps);
                                db.SaveChanges();
                            }
                            #endregion
                            //Caso não tenha parâmetros de saida na tela
                            
                        }
                    }

                    if (arrParamEntradaTela[0] != "")
                    {
                        for (int i = 0; i < arrParamEntradaTela.Length; i++)
                        {
                            string[] arrTempParamEntTela = arrParamEntradaTela[i].Split(',');

                            // Caso tenha parâmetros de entrada no banco
                            #region
                            if (arrParamEntradaBanco[0] != "")
                            {
                                bool temp = false;
                                for (int x = 0; x < arrParamEntradaBanco.Length; x++)
                                {
                                    string[] arrTempParamEntBanco = arrParamEntradaBanco[x].Split(',');
                                    if (arrTempParamEntTela[0] == arrTempParamEntBanco[0])
                                    {
                                        temp = true;
                                    }
                                }
                                if (temp == false)
                                {
                                    ParametroScript ps = new ParametroScript()
                                    {
                                        IdParametro = Int32.Parse(arrTempParamEntTela[0]),
                                        IdScript_CondicaoScript = IdScript_CondicaoScript,
                                        IdTipoParametro = (int)EnumTipoParametro.Input,
                                        ValorDefault = arrTempParamEntTela[3],
                                        Obrigatorio = Convert.ToBoolean(arrTempParamEntTela[1]),
                                        VisivelEmTela = Convert.ToBoolean(arrTempParamEntTela[2])
                                    };
                                    db.ParametroScript.Add(ps);
                                }
                            }
                            #endregion
                        }
                    }

                    if (arrParamSaidaTela[0] != "")
                    {
                        for (int i = 0; i < arrParamSaidaTela.Length; i++)
                        {
                            string[] arrTempParamSaiTela = arrParamSaidaTela[i].Split(',');

                            // Caso tenha parâmetros de saida no banco
                            #region
                            if (arrParamSaidaBanco[0] != "")
                            {
                                bool temp = false;
                                for (int x = 0; x < arrParamSaidaBanco.Length; x++)
                                {
                                    string[] arrTempParamSaiBanco = arrParamSaidaBanco[x].Split(',');
                                    if (arrTempParamSaiTela[0] == arrTempParamSaiBanco[0])
                                    {
                                        temp = true;
                                    }
                                }
                                if (temp == false)
                                {
                                    ParametroScript ps = new ParametroScript()
                                    {
                                        IdParametro = Int32.Parse(arrTempParamSaiTela[0]),
                                        IdScript_CondicaoScript = IdScript_CondicaoScript,
                                        IdTipoParametro = (int)EnumTipoParametro.Output,
                                        ValorDefault = arrTempParamSaiTela[3],
                                        Obrigatorio = Convert.ToBoolean(arrTempParamSaiTela[1]),
                                        VisivelEmTela = Convert.ToBoolean(arrTempParamSaiTela[2])
                                    };
                                    db.ParametroScript.Add(ps);
                                }
                            }
                            #endregion
                        }
                    }

                    db.SaveChanges();
                    retorno = true;
                }
                else
                {
                    #region
                    //Adicionar na tabela ParametroScript os parametros de entrada
                    for (int y = 0; y < listaParametrosEntrada.Count; y++)
                    {
                        ParametroScript ps = new ParametroScript()
                        {
                            IdParametro = listaParametrosEntrada[y].IdParametro,
                            IdScript_CondicaoScript = IdScript_CondicaoScript,
                            IdTipoParametro = (int)EnumTipoParametro.Input,
                            ValorDefault = listaParametrosEntrada[y].ValorDefault,
                            Obrigatorio = listaParametrosEntrada[y].Obrigatorio,
                            VisivelEmTela = listaParametrosEntrada[y].VisivelEmTela
                        };
                        db.ParametroScript.Add(ps);
                        db.SaveChanges();
                        log.Info("Parametro adicionado com sucesso na tabela parametro_script");
                        log.Debug("ParametroScript = " + Util.ToString(ps));
                    }
                    //Adicionar na tabela ParametroScript os parametros de saida
                    for (int i = 0; i < listaParametrosSaida.Count; i++)
                    {
                        ParametroScript ps = new ParametroScript()
                        {
                            IdParametro = listaParametrosSaida[i].IdParametro,
                            IdScript_CondicaoScript = IdScript_CondicaoScript,
                            IdTipoParametro = (int)EnumTipoParametro.Output,
                            ValorDefault = listaParametrosEntrada[i].ValorDefault,
                            Obrigatorio = listaParametrosSaida[i].Obrigatorio,
                            VisivelEmTela = listaParametrosSaida[i].VisivelEmTela
                        };
                        db.ParametroScript.Add(ps);
                        db.SaveChanges();
                        log.Info("Parametro adicionado com sucesso na tabela parametro_script");
                        log.Debug("ParametroScript = " + Util.ToString(ps));
                    }
                    retorno = true;
                    #endregion
                }

            }
            catch (Exception ex)
            {
                retorno = false;
                log.Error(ex);
                this.FlashError("" + ex);
                if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.ToString().Contains("FK_ParametroValor_ParametroScript"))
                {
                    log.Warn("Não é possível excluir parâmetros de scripts que já foram executados.");
                    this.FlashError("Não é possível excluir parâmetros de scripts que já foram executados.");
                }
                else
                {
                    log.Warn("Não foi possível alterar os parâmetros.");
                    this.FlashError(ex.Message);
                }
            }
            return retorno;
        }

        public bool ValidarParametrosScript(int idScript, int idParametro)
        {
            Script_CondicaoScript scs = db.Script_CondicaoScript.Where(x => x.IdScript == idScript).FirstOrDefault();

            int qtdExecs = (from e in db.Execucao
                            join td in db.TestData on e.IdTestData equals td.Id
                            join pv in db.ParametroValor on td.Id equals pv.IdTestData
                            join ps in db.ParametroScript on pv.IdParametroScript equals ps.Id
                            where ps.IdParametro == idParametro && ps.IdScript_CondicaoScript == scs.Id
                            select (td.Id)
                            ).Count();


            //int qtdExecs = db.Execucao.Where(x=>x.IdScript_CondicaoScript_Ambiente == scsa.Id).Count();
            bool retorno = false;

            //se existir execuções para essa Script_CondicaoScript_Ambiente
            if (qtdExecs > 0)
            {
                retorno = true;
            }
            else
            {
                retorno = false;
            }

            return retorno;
        }

        [HttpPost]
        public ActionResult Salvar(ScriptVO objeto, bool editar = false)
        {

            try
            {
                ModelState.Remove("ListaParametrosEntrada");
                ModelState.Remove("ListaParametrosSaida");
                //ModelState.Remove("ListaParametrosEntrada");
                if (!ModelState.IsValid)
                {
                    var msg = string.Empty;
                    ModelState.Values.SelectMany(v => v.Errors).ForEach(m => msg = string.Concat(m.ErrorMessage.ToString(), @"\n"));
                    if (!msg.IsNullOrWhiteSpace())
                        this.FlashWarning(msg);

                    return View("Adicionar", objeto);
                }

                //Captura o usuário logado
                Usuario user = (Usuario)Session["ObjUsuario"];

                //Verifica se é uma ação de Edição
                if (editar)
                {
                    int idScript = objeto.Script.Id;

                    string ambientesExec = Request["arrayAmbientesExec"].ToString();
                    string ambientesVirt = Request["arrayAmbientesVirt"].ToString();
                    //string parametrosEntrada = Request["arrayParametrosEntrada"].ToString();
                    //string parametrosSaida = Request["arrayParametrosSaida"].ToString();

                    objeto.Script.IdAUT = Int32.Parse(Request.Form.Get("listAUT"));
                    string idScriptPai = Request.Form.Get("listScriptPai");

                    if (idScriptPai == "")
                    {
                        objeto.Script.IdScriptPai = null;
                    }
                    else
                    {
                        objeto.Script.IdScriptPai = Int32.Parse(Request.Form.Get("listScriptPai"));
                    }


                    db.Script.Attach(objeto.Script);

                    //Prepara a entidade para uma Edição
                    db.Entry(objeto.Script).State = System.Data.Entity.EntityState.Modified;

                    // informa que o objeto será modificado
                    db.SaveChanges();
                    log.Info("Script editado com sucesso");
                    log.Debug("Script: " + Util.ToString(objeto.Script));

                    Nullable<int> idCondicaoScript;
                    if (Request.Form.Get("listCondicaoScript") == "")
                    {
                        idCondicaoScript = null;
                    }
                    else
                    {
                        idCondicaoScript = Int32.Parse(Request.Form.Get("listCondicaoScript"));
                    }

                    string nomeAUTO = (from aut in db.AUT
                                       where aut.Id == objeto.Script.IdAUT
                                       select aut.Descricao).First();
                    nomeAUTO = nomeAUTO.Replace(" ", "_");
                    nomeAUTO = nomeAUTO.Replace(".", "");

                    string listaExecucao = ConfigurationSettings.AppSettings["ListaExecucaoTosca"];
                    listaExecucao += nomeAUTO + "/" + objeto.Script_CondicaoScript.NomeTecnicoScript;

                    string caminhoArquivoTCS = ConfigurationSettings.AppSettings["CaminhoArquivoTCS"];
                    string nomeAlterado = objeto.Script_CondicaoScript.NomeTecnicoScript.ToLower();
                    nomeAlterado = nomeAlterado.Replace("_", "-");
                    caminhoArquivoTCS += nomeAlterado + ".tcs";

                    string diretorioRelatorio = ConfigurationSettings.AppSettings["DiretorioRelatorio"];

                    Script_CondicaoScript scs = objeto.Script_CondicaoScript;
                    string horaEstimada = "2001-01-01";
                    string tempoEstimado = Request["tempoEstimado"].ToString();
                    tempoEstimado = tempoEstimado.Replace(" ", "");
                    tempoEstimado += ":00.000";
                    string hora = horaEstimada + " " + tempoEstimado;

                    DateTime tempo = DateTime.ParseExact(hora, "yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);

                    scs.ListaExecucaoTosca = listaExecucao;
                    scs.CaminhoArquivoTCS = caminhoArquivoTCS;
                    scs.DiretorioRelatorio = diretorioRelatorio;
                    scs.IdCondicaoScript = idCondicaoScript;
                    scs.IdScript = idScript;
                    scs.QueryTosca = MakeQueryTosca();
                    scs.TempoEstimadoExecucao = tempo;


                    db.Script_CondicaoScript.Attach(objeto.Script_CondicaoScript);

                    //Prepara a entidade para uma Edição
                    db.Entry(objeto.Script_CondicaoScript).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    log.Info("ScriptCondiçãoScript adicionado com sucesso");
                    log.Debug("ScriptCondiçãoScript = " + Util.ToString(objeto.Script_CondicaoScript));

                    inserirAmbientes(scs.Id, true);

                    bool parametros = inserirParametros(getListaParametrosEntrada(), getListaParametrosSaida(), idScript, scs.Id, true);

                    if (parametros == true)
                    {
                        //Retorna uma mensagem de sucesso
                        this.FlashSuccess("Script editado com Sucesso.");
                    }
                    else
                    {
                        //Retorna uma mensagem de sucesso
                        this.FlashError("Não foi possível editar o script pois o mesmo já foi utilizado em alguma execução.");
                    }
                }
                else
                {
                    string nomeTecnicoScript = Request.Form.Get("Script_CondicaoScript.NomeTecnicoScript");

                    //verificando se existe algum registro no banco com o mesmo nome técnico do script
                    int qtdNomeTecnicoBD = (from scso in db.Script_CondicaoScript
                                            where scso.NomeTecnicoScript == nomeTecnicoScript
                                            select scso.NomeTecnicoScript
                                            ).Count();
                    if (qtdNomeTecnicoBD > 0)
                    {
                        this.FlashError("Esse nome técnico já existe, sendo assim, não é possível adicionar um Script com o mesmo nome técnico.");
                    }
                    else
                    {
                        Nullable<int> idCondicaoScript;
                        if (Request.Form.Get("listCondicaoScript") == "")
                        {
                            idCondicaoScript = null;
                        }
                        else
                        {
                            idCondicaoScript = Int32.Parse(Request.Form.Get("listCondicaoScript"));
                        }

                        Script script = new Script();

                        script.IdAUT = Int32.Parse(Request.Form.Get("listaAUT"));
                        //script.NomeTecnicoScript = Request.Form.Get("Script.NomeTecnicoScript");

                        if (Request.Form.Get("listScriptPai") != "")
                            script.IdScriptPai = Int32.Parse(Request.Form.Get("listScriptPai"));

                        script.Descricao = Request.Form.Get("Script.Descricao");

                        objeto.Script = script;

                        #region Validar se Descricao Script já consta na base de dados

                        string desc = Request.Form.Get("Script.Descricao");
                        Script ValidarScript = new Script();
                        ValidarScript = (from s in db.Script
                                         where s.Descricao.StartsWith(desc)
                                         select s
                                         ).FirstOrDefault();

                        if (ValidarScript == null)
                        {
                            db.Script.Add(objeto.Script);
                            //db.SaveChanges();
                            log.Info("Script adicionado com sucesso");
                            log.Debug("Script = " + Util.ToString(script));
                        }
                        else
                        {
                            script = ValidarScript;
                        }

                        #endregion



                        string nomeAUTO = (from aut in db.AUT
                                           where aut.Id == script.IdAUT
                                           select aut.Descricao).First();
                        nomeAUTO = nomeAUTO.Replace(" ", "_");
                        nomeAUTO = nomeAUTO.Replace(".", "");

                        string listaExecucao = ConfigurationSettings.AppSettings["ListaExecucaoTosca"];
                        listaExecucao += nomeAUTO + "/" + nomeTecnicoScript;

                        string caminhoArquivoTCS = ConfigurationSettings.AppSettings["CaminhoArquivoTCS"];
                        string nomeAlterado = nomeTecnicoScript.ToLower();
                        nomeAlterado = nomeAlterado.Replace("_", "-");
                        caminhoArquivoTCS += nomeAlterado + ".tcs";

                        string diretorioRelatorio = ConfigurationSettings.AppSettings["DiretorioRelatorio"];

                        string horaEstimada = "2001-01-01";

                        string tempoEstimado = Request.Form.Get("tempoEstimado");
                        tempoEstimado = tempoEstimado.Replace(" ", "");
                        tempoEstimado += ":00.000";
                        string hora = horaEstimada + " " + tempoEstimado;

                        DateTime tempo = DateTime.ParseExact(hora, "yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);

                        // Adicionar na tabela script_condicao_script
                        Script_CondicaoScript scs = new Script_CondicaoScript()
                        {
                            IdCondicaoScript = idCondicaoScript,
                            IdScript = script.Id,
                            QueryTosca = MakeQueryTosca(),
                            ListaExecucaoTosca = listaExecucao,
                            CaminhoArquivoTCS = caminhoArquivoTCS,
                            DiretorioRelatorio = diretorioRelatorio,
                            TempoEstimadoExecucao = tempo,
                            NomeTecnicoScript = nomeTecnicoScript
                        };
                        db.Script_CondicaoScript.Add(scs);
                        //db.SaveChanges();
                        log.Info("ScriptCondiçãoScript adicionado com sucesso");
                        log.Debug("ScriptCondiçãoScript = " + Util.ToString(scs));

                        //Adicionar os ambientes na tabela de Script_CondicaoScriptAmbiente
                        int IdScript_CondicaoScript = scs.Id;

                        inserirAmbientes(IdScript_CondicaoScript);

                        //db.SaveChanges();

                        bool parametros = inserirParametros(getListaParametrosEntrada(), getListaParametrosSaida(), objeto.Script.Id, scs.Id);
                        if (parametros == true)
                        {
                            this.FlashSuccess("Script adicionado com Sucesso.");
                        }
                        else
                        {
                            this.FlashError("Não foi possível adicionar os parâmetros.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                if (ex.InnerException.InnerException.Message.ToString().Contains("FK_Script_CondicaoScript_Ambiente_Execucao"))
                {
                    log.Warn("Esse ambiente não pode ser excluído, pois já existe execução para esse script, nesse ambiente.");
                    log.Debug("Script = " + Util.ToString(objeto));
                    this.FlashError("Esse ambiente não pode ser excluído, pois já existe execução para esse script, nesse ambiente.");
                }
                else if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.ToString().Contains("IX_Script_Descricao"))
                {
                    log.Warn("Já existe um Script com essa descrição.");
                    log.Debug("Script = " + Util.ToString(objeto));
                    this.FlashError("Já existe um Script com essa descrição.");
                }
                else
                {
                    log.Warn("Já existe um Script com essa descrição.");
                    log.Debug("Script = " + Util.ToString(objeto));
                    this.FlashError(ex.Message);
                }
            }
            return RedirectToAction("Index");
        }
    }
}
