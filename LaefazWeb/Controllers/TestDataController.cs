using LaefazWeb.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TDMWeb.Extensions;
using WebGrease.Css.Extensions;
using TDMWeb.Lib;
using TDMWeb.Models.VOs;
using LaefazWeb.Enumerators;
using Newtonsoft.Json;

namespace LaefazWeb.Controllers
{
    [UsuarioLogado]
    public class TestDataController : Controller
    {

        private DbEntities db = new DbEntities();
        private static IList<ParametroValor> listParametro { get; set; }
        

        public ActionResult Index()
        {
            
            //PEGA O OBJETO COM O USUÁRIO QUE ESTÁ LOGADO
            Usuario user = (Usuario)Session["ObjUsuario"];
            return View();
        }



        public ActionResult Adicionar(int id)
        {
            DataPool dataPool = db.DataPool.Find(id);
            List<ChaveVO> lista = new List<ChaveVO>();

            lista.Add(new ChaveVO("Não", 0));
            lista.Add(new ChaveVO("Sim", 1));
            ViewBag.listStatus = db.Status.ToList();
            ViewBag.listaChaveVO = lista;
            Script_CondicaoScript Script_CondicaoScript = db.Script_CondicaoScript.FirstOrDefault(x => x.Id == dataPool.IdScript_CondicaoScript);
            ViewBag.listScript = db.Script.Where(s => s.Id == Script_CondicaoScript.IdScript).ToList();
            ViewBag.listCondicaoScript = db.CondicaoScript.Where(s => s.Id == Script_CondicaoScript.IdCondicaoScript).ToList();

            return View(dataPool);
        }

        [HttpPost]
        public ActionResult Remover(IList<string> ids)
        {
            var result = new JsonResult() { JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            try
            {
                for(int i=0; i < ids.Count; i++)
                {
                    int idAtual = Int32.Parse(ids[i]);
                    TestData testData = db.TestData.SingleOrDefault(a => a.Id == idAtual);

                    if (testData.IdStatus == (int)EnumStatusTestData.Cadastrada)
                    {

                        //Remove dependencies with table ParametroValor
                        db.ParametroValor.RemoveRange(db.ParametroValor.Where(x => x.IdTestData == idAtual));
                        db.SaveChanges();

                        //Remove register TestData
                        db.TestData.Remove(testData);
                        db.SaveChanges();

                        result.Data = new { Result = "Massa excluída com sucesso.", Status = (int)WebExceptionStatus.Success };
                    }
                    else
                    {
                        result.Data = new { Result = "Não é possível excluir uma massa com status diferente de Cadastrada", Status = (int)WebExceptionStatus.UnknownError };
                    }
                }
            }
            catch (Exception ex)
            {
                result.Data = new { Result = ex.Message, Status = (int)WebExceptionStatus.UnknownError };
            }
            return result;
        }

        public ActionResult Editar(int id)
        {

            List<ChaveVO> lista = new List<ChaveVO>();

            lista.Add(new ChaveVO("Não", 0));
            lista.Add(new ChaveVO("Sim", 1));

            ViewBag.listaChaveVO = lista;

            TestData testData = db.TestData.FirstOrDefault(a => a.Id == id);
            Script_CondicaoScript Script_CondicaoScript = db.Script_CondicaoScript.FirstOrDefault(x => x.Id == testData.IdScript_CondicaoScript);
            ViewBag.listScript = db.Script.Where(s => s.Id == Script_CondicaoScript.IdScript).ToList();
            ViewBag.listCondicaoScript = db.CondicaoScript.Where(s => s.Id == Script_CondicaoScript.IdCondicaoScript).ToList();

            //ViewBag.listScript = db.Script.Where(s => s.IdAUT == testData.DataPool.AUT.Id).ToList();
            //carregaCondicaoScript(testData.Script_CondicaoScript.IdScript+"");
            ViewBag.listStatus = carregaListaStatus(testData.Status.Id);



            return View(testData);
        }
        [HttpPost]
        public ActionResult SalvarEdicao([Bind(Exclude = "Usuario")]TestData objeto)
        {
            try
            {
                Salvar(objeto, true);
                this.FlashSuccess("Massa de teste editada com sucesso.");
            }
            catch (Exception ex)
            {
                this.FlashError(ex.Message);
            }
            return RedirectToAction("Editar/" + objeto.IdDataPool, "DataPool");
        }

        [HttpPost]
        public ActionResult Salvar([Bind(Exclude = "Usuario")]TestData objeto, bool editar = false)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var msg = string.Empty;
                    ModelState.Values.SelectMany(v => v.Errors).ForEach(m => msg = string.Concat(m.ErrorMessage.ToString(), @"\n"));
                    if (!msg.IsNullOrWhiteSpace())
                        this.FlashWarning(msg);

                    return View("Adicionar", objeto);
                }

                  //PEGA O OBJETO COM O USUÁRIO QUE ESTÁ LOGADO
                Usuario user = (Usuario)Session["ObjUsuario"];
                TestData testData;

                // tratamento para impedir que o status seja diferente de cadastrada
                if (Int32.Parse(Request.Form.Get("ListStatus")) != 1)
                {
                    this.FlashError("Não é possível inserir um testData com status diferente de cadastrada!");
                }
                else
                {
                    if (editar)
                    {
                        
                        objeto.Descricao = Request.Form.Get("Descricao") == null ? Request.Form.Get("hidden-descricao") : Request.Form.Get("Descricao");
                        objeto.IdStatus = Request.Form.Get("listStatus") == null ? objeto.IdStatus : Int32.Parse(Request.Form.Get("listStatus"));
                        if (Request.Form.Get("listMigracao") != null)
                            objeto.GerarMigracao = (Int32.Parse(Request.Form.Get("listMigracao")) == 0) ? false : true;
                        objeto.CasoTesteRelativo = Request.Form.Get("CasoTesteRelativo") == null ? Request.Form.Get("hidden-caso-teste-relativo") : Request.Form.Get("CasoTesteRelativo");
                        objeto.Observacao = Request.Form.Get("Observacao") == null ? Request.Form.Get("hidden-observacao") : Request.Form.Get("Observacao");

                        // anexar objeto ao contexto
                        db.TestData.Attach(objeto);


                        db.Entry(objeto).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        //salva parametros
                        SalvaParametros(objeto.Id, objeto.IdScript_CondicaoScript, editar);
                        
                    }
                    else
                    {
                        DateTime dataAtual = DateTime.Today;
                        if ((DateTime)Convert.ToDateTime(Request.Form.Get("DataSolicitacao")) < dataAtual)
                        {
                            this.FlashError("Não é permitido selecionar uma Data de Geração Solicitada menor do que a data atual");
                        }
                        else
                        {
                            testData = new TestData()
                            {
                                Descricao = Request.Form.Get("DescricaoTestData"),
                                IdStatus = Int32.Parse(Request.Form.Get("ListStatus")),
                                IdScript_CondicaoScript = objeto.IdScript_CondicaoScript,
                                IdDataPool = objeto.Id,
                                CasoTesteRelativo = Request.Form.Get("CasoTesteRelativo"),
                                GerarMigracao = Int32.Parse(Request.Form.Get("ListMigracao")) == 1 ? true : false,
                                Observacao = Request.Form.Get("observacao"),
                                IdUsuario = user.Id
                            };

                            db.TestData.Add(testData);
                            db.SaveChanges();

                            //Salvar parametros
                            SalvaParametros(testData.Id, testData.IdScript_CondicaoScript, editar);

                            this.FlashSuccess("Massa de teste adicionada com sucesso.");
                        }                        
                    }
                }                
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.ToString().Contains("AK_TDM_Descricao"))
                    this.FlashError("Já existe uma Massa de Teste com essa descrição.");
                else
                    this.FlashError(ex.Message);
            }

            return RedirectToAction("Editar/" + objeto.Id, "DataPool");
        }


        private void SalvaParametros(int idTestData, int scriptCondicaoScript, bool edicao)
        {
            DbEntities db = new DbEntities();

            List<ParametroScript> listParametroScript = db.ParametroScript.Where(x => x.IdScript_CondicaoScript == scriptCondicaoScript).ToList();
            List<ParametroValor> listParametroValor = new List<ParametroValor>();
            List<ParametroVO> listParametroVO = new List<ParametroVO>();

            foreach (ParametroScript ps in listParametroScript)
            {
                Parametro p = db.Parametro.Where(x => x.Id == ps.IdParametro).FirstOrDefault();
                ParametroValor pv = null;
                if (edicao)
                {
                    pv = db.ParametroValor.Where(x => x.IdTestData == idTestData && x.IdParametroScript == ps.Id).FirstOrDefault();
                    pv.Valor = Request.Form.Get(p.Descricao) == null ? "" : Request.Form.Get(p.Descricao);

                    db.ParametroValor.Attach(pv);
                    db.Entry(pv).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    pv = new ParametroValor
                    {
                        IdParametroScript = ps.Id,
                        IdTestData = idTestData,
                        Valor = Request.Form.Get(p.Descricao) == null ? "" : Request.Form.Get(p.Descricao)
                    };

                    db.ParametroValor.Add(pv);
                    db.SaveChanges();
                }

                
            }
        }



        public JsonResult carregaCondicaoScript(string id)
        {
            int idScript = Int32.Parse(id);

            List<Script_CondicaoScript> listaScript_CondicaoScript = db.Script_CondicaoScript.Where(x => x.IdScript == idScript).ToList();

            List<CondicaoScript> listaCondicaoScript = new List<CondicaoScript>();
            for (int i = 0; i < listaScript_CondicaoScript.Count; i++)
            {
                CondicaoScript condicaoScript = db.CondicaoScript.Find(listaScript_CondicaoScript[i].IdCondicaoScript);
                if (condicaoScript != null)
                    listaCondicaoScript.Add(condicaoScript);
            }

            ViewBag.listCondicaoScript = listaCondicaoScript;

            Dictionary<string, string> ListaCondicao = new Dictionary<string, string>();

            foreach (var item in listaCondicaoScript)
            {
                ListaCondicao.Add(item.Id + "", item.Descricao);
            }
            string json = JsonConvert.SerializeObject(ListaCondicao, Formatting.Indented);


            return Json(json, JsonRequestBehavior.AllowGet);
        }

        private List<Status> carregaListaStatus(int idStatus)
        {

            List<Status> listaStatus = new List<Status>();

            switch (idStatus)
            {
                case (int)EnumStatusTestData.Cadastrada:
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Cadastrada));
                    break;
                case (int)EnumStatusTestData.EmGeracao:
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.EmGeracao));
                    break;
                case (int)EnumStatusTestData.Disponivel:
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Disponivel));
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Utilizada));
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Reservada));
                    break;
                case (int)EnumStatusTestData.Reservada:
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Disponivel));
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Utilizada));
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Reservada));
                    break;
                case (int)EnumStatusTestData.Utilizada:
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Utilizada));
                    break;
                case (int)EnumStatusTestData.Falha:
                    listaStatus.Add(db.Status.Find((int)EnumStatusTestData.Falha));
                    break;
            }

            return listaStatus;
        }


        public JsonResult DisponibilizarMassaDataPoolPublico(IList<string> ids)
        {
            var result = new JsonResult() { JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            try
            {
                for (int i=0; i<ids.Count; i++)
                {
                    int idAtual = Int32.Parse(ids[i]);

                    TestData TestData = db.TestData.FirstOrDefault(x => x.Id == idAtual);

                    if (TestData.IdStatus == (int)EnumStatusTestData.Disponivel || TestData.IdStatus == (int)EnumStatusTestData.Reservada)
                    {
                        TDM TDM = db.TDM.FirstOrDefault(x => x.TdmPublico == true);
                        List<DataPool> DataPool = db.DataPool.Where(x => x.IdTDM == TDM.Id).Where(x => x.IdScript_CondicaoScript == TestData.IdScript_CondicaoScript).Where(x => x.IdAut == TestData.DataPool.IdAut).ToList();

                        //VERIFICA SE EXISTE ALGUM DATAPOOL COM O MESMO CONJUNTO SCRIPT E CONDIÇÃO
                        if (DataPool.Count() == 0)
                        {
                            DataPool DataPoolCurrent = db.DataPool.Where(x => x.Id == TestData.IdDataPool).FirstOrDefault();
                            DataPool DataPoolPublic = new DataPool()
                            {

                                IdTDM = TDM.Id,
                                IdScript_CondicaoScript = TestData.IdScript_CondicaoScript,
                                IdAut = DataPoolCurrent.IdAut,
                                Observacao = DataPoolCurrent.Observacao,
                                Descricao = "DATAPOOL PÚBLICO " + TestData.DataPool.AUT.Descricao,
                                DataSolicitacao = DateTime.Now,
                                QtdSolicitada = 0
                            };

                            db.DataPool.Add(DataPoolPublic);
                            db.SaveChanges();

                            DataPool = db.DataPool.Where(x => x.IdTDM == TDM.Id).Where(x => x.IdScript_CondicaoScript == TestData.IdScript_CondicaoScript).ToList();
                        }


                        int IdDataPoolSystem = DataPool.FirstOrDefault(x => x.IdAut == TestData.DataPool.IdAut).Id;

                        TestData.IdDataPool = IdDataPoolSystem;
                        TestData.IdStatus = (int)EnumStatusTestData.Disponivel;

                        SalvarObjeto(TestData);

                        result.Data = new { Result = "Massa(s) de Teste disponibilizada(s) para TDM público com sucesso.", Status = (int)WebExceptionStatus.Success };
                        //this.FlashSuccess("Massa de Teste disponibilizada para TDM público com sucesso.");
                    }
                    else
                    {
                        result.Data = new { Result = "Não é possível disponibilizar massas com status diferente de Disponível e Reservada.", Status = (int)WebExceptionStatus.UnknownError };
                        //this.FlashError("Não é possível disponibilizar massas com status diferente de Disponível e Reservada.");
                    }
                }
                
            }
            catch (Exception ex)
            {
                result.Data = new { Result = ex.Message, Status = (int)WebExceptionStatus.UnknownError };
            }
            return result;
        }


        [HttpPost]
        public ActionResult SalvarObjeto(TestData objeto)
        {
            try
            {

                // anexar objeto ao contexto
                db.TestData.Attach(objeto);
                db.Entry(objeto).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.ToString().Contains("AK_TDM_Descricao"))
                    this.FlashError("Já existe uma Massa de Teste com essa descrição.");
                else
                    this.FlashError(ex.Message);
            }

            return RedirectToAction("Index");
        }

        public ActionResult MultiplicarTestData()
        {
            int Id = Int32.Parse(Request.Form.Get("TestDataId"));
            int QtdMassas = Int32.Parse(Request.Form.Get("QtdMassa"));
            TestData TestData = db.TestData.Where(x => x.Id == Id).FirstOrDefault();

            try
            {

                for (int i = 0; i < QtdMassas; i++)
                {

                    TestData TesteDataTemp = new TestData()
                    {
                        IdDataPool = TestData.IdDataPool,
                        IdStatus = (int)EnumStatusTestData.Cadastrada,
                        IdScript_CondicaoScript = TestData.IdScript_CondicaoScript,
                        IdUsuario = TestData.IdUsuario,
                        Descricao = TestData.Descricao,
                        GerarMigracao = TestData.GerarMigracao,
                        CasoTesteRelativo = TestData.CasoTesteRelativo,
                        Observacao = TestData.Observacao,
                    };

                    db.TestData.Add(TesteDataTemp);
                    db.SaveChanges();

                    List<ParametroValor> parametros = db.ParametroValor.Where(x => x.IdTestData == Id).ToList();

                    foreach (ParametroValor item in parametros)
                    {

                        ParametroValor parametroValor = new ParametroValor()
                        {
                            IdTestData = TesteDataTemp.Id,
                            IdParametroScript = item.IdParametroScript,
                            ParametroScript = item.ParametroScript,
                            Valor = item.Valor
                        };

                        db.ParametroValor.Add(parametroValor);
                        db.SaveChanges();
                    }
                }

                this.FlashSuccess("Massas de teste adicionadas com sucesso.");
            }
            catch (Exception ex)
            {
                this.FlashError(ex.Message);
            }

            return RedirectToAction("Editar/" + TestData.IdDataPool, "DataPool");
        }



        public JsonResult CarregarParametroValor(string idScriptCondicaoScript, string idTestData)
        {
            DbEntities db = new DbEntities();
            int scriptCondicaoScript = Int32.Parse(idScriptCondicaoScript);
            int id = Int32.Parse(idTestData);

            List<ParametroScript> listParametroScript = db.ParametroScript.Where(x => x.IdScript_CondicaoScript == scriptCondicaoScript).ToList();
            List<ParametroValorVO> listParametroValorVO = new List<ParametroValorVO>();

            int IdStatus = db.TestData.Where(x => x.Id == id).FirstOrDefault().IdStatus;

            foreach (ParametroScript ps in listParametroScript)
            {
                Parametro p = db.Parametro.Where(x => x.Id == ps.IdParametro).FirstOrDefault();

                //realiza busca dos valores dos parâmetros associados ao testdata, caso não seja encontrado será adicionado a lista como vazio
                ParametroValor pv = db.ParametroValor.Where(x => x.IdParametroScript == ps.Id && x.IdTestData == id).FirstOrDefault();
                if (pv == null)
                {
                    listParametroValorVO.Add(new ParametroValorVO { IdStatusTestData = IdStatus, Valor = "", Id = 0, IdParametroScript = ps.Id, Descricao = p.Descricao, Tipo = p.Tipo, IdParametro = p.Id, IdTipoParametro = ps.IdTipoParametro, Obrigatorio = ps.Obrigatorio });
                }
                else
                {
                    listParametroValorVO.Add(new ParametroValorVO { IdStatusTestData = IdStatus, Valor = pv.Valor, Id = pv.Id, IdParametroScript = ps.Id, Descricao = p.Descricao, Tipo = p.Tipo, IdParametro = p.Id, IdTipoParametro = ps.IdTipoParametro, Obrigatorio = ps.Obrigatorio });
                }
            }

            string json = JsonConvert.SerializeObject(listParametroValorVO, Formatting.Indented);

            return Json(json, JsonRequestBehavior.AllowGet);
        }

            public JsonResult CarregarParametros(string idScriptCondicaoScript, string idTestData)
        {
            DbEntities db = new DbEntities();
            int scriptCondicaoScript = Int32.Parse(idScriptCondicaoScript);
            int id = Int32.Parse(idTestData);

            List<ParametroScript> listParametroScript = db.ParametroScript.Where(x => x.IdScript_CondicaoScript == scriptCondicaoScript).ToList();
            List<ParametroValorVO> listParametroValorVO = new List<ParametroValorVO>();
            List<ParametroVO> listParametroVO = new List<ParametroVO>();

            foreach (ParametroScript ps in listParametroScript)
            {
                Parametro p = db.Parametro.Where(x => x.Id == ps.IdParametro).FirstOrDefault();

                listParametroVO.Add(new ParametroVO { IdParametro = p.Id, Descricao = p.Descricao, IdParametroScript = ps.Id, Tipo = p.Tipo, IdTipoParametro = ps.IdTipoParametro });

            }

            string json = JsonConvert.SerializeObject(listParametroVO, Formatting.Indented);
                       
            return Json(json, JsonRequestBehavior.AllowGet);

        }


      


    }

   






    }
