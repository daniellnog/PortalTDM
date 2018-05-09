using LaefazWeb.Models;
using LaefazWeb.Models.VOs;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TDMWeb.Extensions;
using WebGrease.Css.Extensions;
using TDMWeb.Lib;
using System.Web.Services;
using LaefazWeb.Enumerators;
using Newtonsoft.Json;
using System.IO;
using System.Data;
using System.Globalization;
using System.Data.Entity.Validation;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;

namespace LaefazWeb.Controllers
{
    [UsuarioLogado]
    public class DataPoolController : Controller, IEquatable<DataPoolController>
    {
#if (DEBUG)
        public static string SqlConnectionString = @"Data Source=localhost;Database=TDM.Db;Trusted_Connection=true;Persist Security Info=True";
#else
        public static string SqlConnectionString = ConfigurationManager.ConnectionStrings["BulkInsert"].ConnectionString;
#endif


        private DbEntities db = new DbEntities();

        public ActionResult Index()
        {

            //ViewBag.url = System.Configuration.ConfigurationManager.AppSettings["UrlHome"];

            //PEGA OS TDMs ASSOCIADOS AO USUÁRIO LOGADO
            List<TDM> listaTDM = getListTDMByUser();

            //ORDENA A LISTA PELO ATRIBUTO TDMPUBLICO
            //listaTDM.Sort((o1, o2) => -1* o1.TdmPublico.CompareTo(o2.TdmPublico));
            if (TempData["IdTDM"] != null)
                ViewBag.IdTdm = TempData["IdTDM"].ToString();

            if (TempData["DescDemanda"] != null)
                ViewBag.DescDemanda = TempData["DescDemanda"].ToString();

            listaTDM = listaTDM.OrderBy(s => s.Descricao).OrderByDescending(s => s.TdmPublico).ToList();

            TDM allTDM = new TDM()
            {
                Id = 0,
                Descricao = "TODOS",
                TdmPublico = false
            };

            listaTDM.Add(allTDM);
            ViewBag.ListaTDM = listaTDM;
            ViewBag.ListaTDM = listaTDM.OrderBy(s => s.Descricao).OrderByDescending(s => s.TdmPublico);

            ViewBag.ListaAmbienteExec = "";

            ViewBag.ListaAmbienteVirt = "";

            ViewBag.ListaTipoFaseTeste = db.TipoFaseTeste.ToList();

            getMinhasDemandas();
            return View(db.DataPool.ToList());
        }

        public ActionResult IndexDataPool(string id)
        {
            TempData["IdTDM"] = id;
            return RedirectToAction("Index");
        }

        public ActionResult DataPoolDemanda(string id, string descDemanda)
        {
            TempData["DescDemanda"] = descDemanda;
            TempData["IdTDM"] = id;
            return RedirectToAction("Index");
        }

        DateTime actualDate = DateTime.Now;

        [HttpPost]
        public JsonResult ImportarArquivo()
        {
            var result = new JsonResult() { JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            try
            {
                var idDataPool = Request.Form["idDataPool"];
                if (Request.Files.Count > 0)
                {
                    var file = Request.Files[0];
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var path = Path.Combine(Server.MapPath("~/Uploads/"), fileName);
                        file.SaveAs(path);

                        SalvarImportacao(path, Convert.ToInt32(idDataPool));

                        //Remove o arquivo
                        System.IO.File.Delete(path);

                        result.Data = new { Result = "Arquivo importado com sucesso.", Status = (int)WebExceptionStatus.Success };
                    }
                    else
                    {
                        result.Data = new { Result = "Arquivo informado está vazio.", Status = (int)WebExceptionStatus.UnknownError };
                    }
                }
                else
                {
                    result.Data = new { Result = "Nenhum arquivo encontrado.", Status = (int)WebExceptionStatus.UnknownError };
                }

            }
            catch (Exception ex)
            {
                result.Data = new { Result = ex.Message, Status = (int)WebExceptionStatus.UnknownError };
            }

            return result;
        }

        
        public void AlterarStatusTestData(string IdTestData)
        {
            int testDataId = Int32.Parse(IdTestData);
            TestData testData = db.TestData.FirstOrDefault(a => a.Id == testDataId);
            //alterando o status para UTILIZADA
            testData.IdStatus = (int)EnumStatusTestData.Utilizada;
            // anexar objeto ao contexto
            db.TestData.Attach(testData);
            db.Entry(testData).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
        }

        // método para atualizar os campos de Qtd Disponivel, Qtd Reservada, Qtd Utilizada
        public JsonResult AtualizarQtdsMassasDatapool (string IdDatapool)
        {
            int idDataPool = Int32.Parse(IdDatapool);

            int[] qtds = new int[3];

            int qtdDisponivel = (from qtd in db.TestData
                            where qtd.IdDataPool.Equals(idDataPool) && qtd.IdStatus.Equals((int)EnumStatusTestData.Disponivel)
                            select qtd).ToList().Count();

            int qtdUtilizada = (from qtd in db.TestData
                            where qtd.IdDataPool.Equals(idDataPool) && qtd.IdStatus.Equals((int)EnumStatusTestData.Utilizada)
                            select qtd).ToList().Count();

            int qtdReservada = (from qtd in db.TestData
                             where qtd.IdDataPool.Equals(idDataPool) && qtd.IdStatus.Equals((int)EnumStatusTestData.Reservada)
                             select qtd).ToList().Count();


            Dictionary<string, int> dic = new Dictionary<string, int>();
            dic.Add("qtdUtilizada", qtdUtilizada);
            dic.Add("qtdDisponivel", qtdDisponivel);
            dic.Add("qtdReservada", qtdReservada);


            string json = JsonConvert.SerializeObject(dic, Formatting.Indented);

            return (Json(json, JsonRequestBehavior.AllowGet));

        }

        private void SalvarImportacao(string path, int idDataPool)
        {
            DataTable dt;

            if(!Util.ValidaPlanilhaExcel(path, idDataPool))
            {
                throw new Exception("Esta planilha não corresponde ao DataPool selecionado, favor verificar os campos 'Sistema','Script' e 'Condição' na planilha.");
            }

            dt = Util.LerExcel(path);

            List<Coluna> ValidacaoColuna = (from datapool in db.DataPool
                                            join scs in db.Script_CondicaoScript on datapool.IdScript_CondicaoScript equals scs.Id
                                            join paramscript in db.ParametroScript on scs.Id equals paramscript.IdScript_CondicaoScript
                                            join param in db.Parametro on paramscript.IdParametro equals param.Id
                                            where datapool.Id == idDataPool && paramscript.IdTipoParametro == (int)EnumTipoParametroScript.Input
                                            select new Coluna
                                            {
                                                IdParametroScript = paramscript.Id,
                                                Parametro = param.Descricao,
                                                Tipo = param.Tipo,
                                                Obrigatorio = paramscript.Obrigatorio,
                                                Script_CondicaoScript = scs.IdCondicaoScript,
                                                DescricaoTestData = datapool.Descricao
                                            }).ToList();

            foreach (DataColumn column in dt.Columns)
            {
                var Registro = (from item in ValidacaoColuna
                                where item.Parametro == column.Caption
                                select new
                                {
                                    Nulo = item.Obrigatorio,
                                    Tipo = item.Tipo
                                }).FirstOrDefault();

                if (Registro != null)
                {
                    bool obrigatorio = bool.Parse(Registro.Nulo.ToString());


                    int contRow = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (obrigatorio && row[column.Caption].ToString().IsNullOrWhiteSpace())
                        {
                            throw new Exception("O Parâmetro '" + column.Caption + "' é obrigatório e não foi preenchido na linha " + contRow);
                        }
                        else if (Registro.Tipo == "NUMBER")
                        {
                            long NumeroValido;
                            bool retorno = Int64.TryParse(row[column.Caption].ToString(), out NumeroValido);

                            if (!retorno)
                            {
                                throw new Exception("A coluna '"+ column.Caption + "' não está preenchida com valor numérico na linha "+ contRow);
                            }
                        }
                        else if (Registro.Tipo == "DATE")
                        {
                            string Dateformat = "dd/MM/yyyy";
                            CultureInfo cult = new CultureInfo("pt-BR");
                            DateTime DataValida = DateTime.MinValue;

                            bool retorno = DateTime.TryParseExact(row[column.Caption].ToString().Substring(0, 10), Dateformat, cult, DateTimeStyles.None, out DataValida);

                            if (!retorno)
                            {
                                throw new Exception("A coluna '" + column.Caption + "' não está preenchida com data na linha " + contRow);
                            }
                        }
                        contRow++;
                    }
                }
            }

            if (dt != null)
            {
                InserirDados(dt, idDataPool, ValidacaoColuna);
            }
        }

        // Método para leitura do mapa de calor na visualização de testData
        [HttpPost]
        public void lerMapaCalorTestData(string strX, string strY, string data, string res)
        {
            string[] arrX = strX.Split(',');
            string[] arrY = strY.Split(',');
            string[] arrDate = data.Split(',');

            Usuario user = (Usuario)Session["ObjUsuario"];

            for (int i = 0; i < arrX.Length; i++)
            {
                switch (res)
                {
                    case "1920x1080":
                        MapaCalor mapa1920x1080 = new MapaCalor
                        {
                            PosX = ((Int32.Parse(arrX[i]) * 800) / 1920),
                            PosY = ((Int32.Parse(arrY[i]) * 381) / 963),
                            IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                            IdUsuario = user.Id,
                            Data = DateTime.Parse(arrDate[i]),
                            Resolucao = "1920x1080"
                        };
                        db.MapaCalor.Add(mapa1920x1080);
                        if (Int32.Parse(arrY[i]) > 910 || Int32.Parse(arrY[i]) < 150)
                        {
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 560) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 257) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };

                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 560) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 257) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };

                            db.MapaCalor.Add(mapa1600x900_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        else
                        {
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 660) / 1600),
                                PosY = ((Int32.Parse(arrY[i]) * 390) / 782),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };
                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 560) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 377) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };
                            db.MapaCalor.Add(mapa1600x900_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        break;

                    case "1600x900":
                        MapaCalor mapa1600x900 = new MapaCalor
                        {
                            PosX = ((Int32.Parse(arrX[i]) * 800) / 1600),
                            PosY = ((Int32.Parse(arrY[i]) * 381) / 782),
                            IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                            IdUsuario = user.Id,
                            Data = DateTime.Parse(arrDate[i]),
                            Resolucao = "1600x900"
                        };
                        db.MapaCalor.Add(mapa1600x900);
                        // FOOTER AND HEADER
                        if (Int32.Parse(arrY[i]) > 729 || Int32.Parse(arrY[i]) < 50)
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 980) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 465) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 680) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 320) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        else
                        // FORA DO FOOTER AND HEADER
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 965) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 670) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        break;
                    case "1366x768":
                        MapaCalor mapa1366x768 = new MapaCalor
                        {
                            PosX = ((Int32.Parse(arrX[i]) * 800) / 1351),
                            PosY = ((Int32.Parse(arrY[i]) * 381) / 649),
                            IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                            IdUsuario = user.Id,
                            Data = DateTime.Parse(arrDate[i]),
                            Resolucao = "1366x768"
                        };
                        db.MapaCalor.Add(mapa1366x768);
                        // DENTRO DO FOOTER AND HEADER
                        if (Int32.Parse(arrY[i]) > 615 || Int32.Parse(arrY[i]) < 50)
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 1160) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 565) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 960) / 1600),
                                PosY = ((Int32.Parse(arrY[i]) * 450) / 782),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1600x900_1);
                        }
                        else
                        // FORA DO FOOTER AND HEADER
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 1160) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 960) / 1600),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 782),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.TestData,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1600x900_1);
                        }
                        break;
                }
            }
            db.SaveChanges();
        }




        // Método para leitura do mapa de calor na visualização de Datapool
        [HttpPost]
        public void lerMapaCalor(string strX, string strY, string data, string res)
        {
            string[] arrX = strX.Split(',');
            string[] arrY = strY.Split(',');
            string[] arrDate = data.Split(',');

            Usuario user = (Usuario)Session["ObjUsuario"];            

            for (int i = 0; i < arrX.Length; i++)
            {
                switch (res)
                {
                    case "1920x1080":
                        MapaCalor mapa1920x1080 = new MapaCalor
                        {
                            PosX = ((Int32.Parse(arrX[i]) * 800) / 1920),
                            PosY = ((Int32.Parse(arrY[i]) * 381) / 963),
                            IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                            IdUsuario = user.Id,
                            Data = DateTime.Parse(arrDate[i]),
                            Resolucao = "1920x1080"
                        };
                        db.MapaCalor.Add(mapa1920x1080);
                        if (Int32.Parse(arrY[i])>910 || Int32.Parse(arrY[i]) < 150)
                        {
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 560) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 257) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };

                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 560) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 257) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };

                            db.MapaCalor.Add(mapa1600x900_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        else
                        {
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 660) / 1600),
                                PosY = ((Int32.Parse(arrY[i]) * 390) / 782),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };
                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 560) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 377) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };
                            db.MapaCalor.Add(mapa1600x900_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        break;

                    case "1600x900":
                        MapaCalor mapa1600x900 = new MapaCalor
                        {
                            PosX = ((Int32.Parse(arrX[i]) * 800) / 1600),
                            PosY = ((Int32.Parse(arrY[i]) * 381) / 782),
                            IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                            IdUsuario = user.Id,
                            Data = DateTime.Parse(arrDate[i]),
                            Resolucao = "1600x900"
                        };
                        db.MapaCalor.Add(mapa1600x900);
                        // FOOTER AND HEADER
                        if (Int32.Parse(arrY[i]) > 729 || Int32.Parse(arrY[i]) < 50)
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 980) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 465) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 680) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 320) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        else
                        // FORA DO FOOTER AND HEADER
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 965) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1366x768_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 670) / 1351),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 649),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1366x768"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1366x768_1);
                        }
                        break;
                    case "1366x768":
                        MapaCalor mapa1366x768 = new MapaCalor
                        {
                            PosX = ((Int32.Parse(arrX[i]) * 800) / 1351),
                            PosY = ((Int32.Parse(arrY[i]) * 381) / 649),
                            IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                            IdUsuario = user.Id,
                            Data = DateTime.Parse(arrDate[i]),
                            Resolucao = "1366x768"
                        };
                        db.MapaCalor.Add(mapa1366x768);
                        // DENTRO DO FOOTER AND HEADER
                        if (Int32.Parse(arrY[i]) > 615 || Int32.Parse(arrY[i]) < 50)
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 1160) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 565) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 960) / 1600),
                                PosY = ((Int32.Parse(arrY[i]) * 450) / 782),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1600x900_1);
                        }
                        else
                        // FORA DO FOOTER AND HEADER
                        {
                            MapaCalor mapa1920x1080_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 1160) / 1920),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 963),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1920x1080"
                            };
                            MapaCalor mapa1600x900_1 = new MapaCalor
                            {
                                PosX = ((Int32.Parse(arrX[i]) * 960) / 1600),
                                PosY = ((Int32.Parse(arrY[i]) * 380) / 782),
                                IdTelaMapaCalor = (int)EnumTelaMapaCalor.DataPool,
                                IdUsuario = user.Id,
                                Data = DateTime.Parse(arrDate[i]),
                                Resolucao = "1600x900"
                            };

                            db.MapaCalor.Add(mapa1920x1080_1);
                            db.MapaCalor.Add(mapa1600x900_1);
                        }
                        break;
                }
                
            }

            db.SaveChanges();
        }

        public void InserirDados(DataTable dr, int idDataPool, List<Coluna> ValidacaoColuna)
        {
            DataPool pool = new DataPool();
            pool = db.DataPool.Where(d => d.Id == idDataPool).FirstOrDefault();

            if (dr != null && dr.Rows.Count > 0)
            {
                foreach (DataRow row in dr.Rows)
                {
                    Usuario user = (Usuario)Session["ObjUsuario"];
                    TestData test = new TestData();
                    test.IdStatus = (int)EnumStatusTestData.Cadastrada;
                    test.IdScript_CondicaoScript = pool.IdScript_CondicaoScript;
                    test.Descricao = pool.Descricao;
                    test.IdUsuario = user.Id;

                    foreach (DataColumn column in dr.Columns)
                    {
                        if (column.Caption.Equals("Gerar antes de código migrado?"))
                        {
                            test.GerarMigracao = row[column.Caption].ToString().ToUpper() == "SIM" ? true : false;
                        }
                        else if (column.Caption.Equals("Caso de Teste"))
                        {
                            test.CasoTesteRelativo = row[column.Caption].ToString();
                        }
                        else if (column.Caption.Equals("Observações"))
                        {
                            test.Observacao = row[column.Caption].ToString();
                        }

                        if (ValidacaoColuna.Where(item => item.Parametro == column.Caption).FirstOrDefault() != null)
                            test.ParametroValor.Add(new ParametroValor { IdParametroScript = ValidacaoColuna.Where(item => item.Parametro == column.Caption).FirstOrDefault().IdParametroScript, Valor = row[column.Caption].ToString() });
                    }

                    pool.TestData.Add(test);
                }
            }

            db.SaveChanges();
        }

        public JsonResult CarregarDataPool()
        {
            List<DataPoolVO> DataPoolVOs = new List<DataPoolVO>();

            // get Start (paging start index) and length (page size for paging)
            string draw = Request.Form.GetValues("draw").FirstOrDefault();
            string start = Request.Form.GetValues("start").FirstOrDefault();
            string length = Request.Form.GetValues("length").FirstOrDefault();
            //Get Sort columns value
            string sortColumn = Request.Form.GetValues("order[0][column]").FirstOrDefault();
            string sortColumnDir = Request.Form.GetValues("order[0][dir]").FirstOrDefault();
            string searchValue = Request.Form.GetValues("search[value]").FirstOrDefault();
            string idTDM = Request.Form.Get("idTDM");


            Usuario user = (Usuario)Session["ObjUsuario"];

            int IdUsuario = user.Id;

            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int TotalRows = 0;

            if (!String.IsNullOrEmpty(idTDM))
            {
                SqlParameter[] param =
                {
                    new SqlParameter("@DisplayLength", length),
                    new SqlParameter("@DisplayStart", start),
                    new SqlParameter("@SortCol", sortColumn),
                    new SqlParameter("@SortDir", sortColumnDir),
                    new SqlParameter("@SEARCH", DBNull.Value),
                    new SqlParameter("@ListarTodos", 1),
                    new SqlParameter("@IdUsuario", IdUsuario),
                    new SqlParameter("@IdTDM", DBNull.Value)
                };

                if (searchValue != "")
                    param[4].Value = searchValue;

                if (idTDM != "0")
                    param[7].Value = idTDM;


                DataPoolVOs = db.Database.SqlQuery<DataPoolVO>(
                        "EXEC PR_LISTAR_DATAPOOL @DisplayLength, @DisplayStart, @SortCol, @SortDir, @SEARCH, @ListarTodos, @IdUsuario, @IdTDM ", param).ToList();

                //List<TestData> listaMassasTestes = new List<TestData>();
                //listaMassasTestes.AddRange(db.TestData.Where(x => x.IdDataPool == IdDataPool).ToList());

                //DataPoolVOs.ListaMassa.ad
                //MassaTesteVOs = db.Database.SqlQuery<MassaTesteVO>(
                //        "EXEC PR_LISTAR_MASSA_TESTE @DisplayLength, @DisplayStart, @SortCol, @SortDir, @SEARCH, @IdDataPool ", param).ToList();

                //ViewBag.ListaMassaTeste = listaMassasTestes.OrderBy(s => s.Descricao);


                if (DataPoolVOs.Any())
                    TotalRows = DataPoolVOs.FirstOrDefault().TotalCount;

            }

            if (!idTDM.IsNullOrWhiteSpace())
            {
                int id = Int32.Parse(idTDM);
                TDM TDM = db.TDM.FirstOrDefault(a => a.Id == id);

                //Regras dos Faróis DataPool
                if (id != 0 && TDM.TdmPublico)
                {

                    foreach (var item in DataPoolVOs)
                    {
                        if (item.QtdDisponivel > 0)
                        {
                            item.Farol = (int)EnumFarol.Verde;

                        }
                        else
                        {
                            item.Farol = (int)EnumFarol.Cinza;
                        }
                    }

                }
                else
                {

                    foreach (var item in DataPoolVOs)
                    {
                        if ((actualDate.AddDays(14) < item.DataSolicitacao) ||  item.DataTermino < actualDate.Date)
                        {
                            item.Farol = (int)EnumFarol.Cinza;
                        }
                        else if (item.QtdDisponivel < item.QtdSolicitada)
                        {
                            item.Farol = (int)EnumFarol.Vermelho;

                        }
                        else if ((item.QtdDisponivel > item.QtdSolicitada) && (item.QtdDisponivel < (item.QtdSolicitada * 1.2)))
                        {
                            item.Farol = (int)EnumFarol.Amarelo;

                        }
                        else
                        {
                            item.Farol = (int)EnumFarol.Verde;
                        }
                    }
                }


            }


            if (Int32.Parse(sortColumn) == 7 && sortColumnDir.Equals("desc"))
            {
                DataPoolVOs = DataPoolVOs.OrderByDescending(x => x.Farol).ToList();
            }
            else if (Int32.Parse(sortColumn) == 7 && sortColumnDir.Equals("asc"))
            {
                DataPoolVOs = DataPoolVOs.OrderBy(x => x.Farol).ToList();
            }


            return Json(new { draw = draw, recordsFiltered = TotalRows, recordsTotal = TotalRows, data = DataPoolVOs }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CarregaDataPoolDemanda(string id, string IdDemamnda)
        {
            int Id = Int32.Parse(id);
            int IdDemanda = IdDemamnda == null?0:Int32.Parse(IdDemamnda);

            Demanda Demanda = db.Demanda.FirstOrDefault(x => x.Id == IdDemanda);

            if (Id == 0)
            {
                TempData["DescDemanda"] = Demanda.Descricao;
                TempData["IdTDM"] = id;
            }
            else
            {

                
                TempData["DescDemanda"] = Demanda.Descricao;
                TempData["IdTDM"] = id;
            }
            return RedirectToAction("Index");

        }

        //public JsonResult CarregarMassaDeTestes(int IdDataPool)
        //{
        //    //List<MassaTesteVO> MassaTesteVOs = new List<MassaTesteVO>();

        //    //// get Start (paging start index) and length (page size for paging)
        //    string draw = Request.Form.GetValues("draw").FirstOrDefault();
        //    //string start = Request.Form.GetValues("start").FirstOrDefault();
        //    //string length = Request.Form.GetValues("length").FirstOrDefault();
        //    ////Get Sort columns value
        //    //string sortColumn = Request.Form.GetValues("order[0][column]").FirstOrDefault();
        //    //string sortColumnDir = Request.Form.GetValues("order[0][dir]").FirstOrDefault();
        //    //string searchValue = Request.Form.GetValues("search[value]").FirstOrDefault();
        //    //string idTDM = Request.Form.Get("idTDM");

        //    //int pageSize = length != null ? Convert.ToInt32(length) : 0;
        //    //int skip = start != null ? Convert.ToInt32(start) : 0;
        //    int TotalRows = 0;


        //    //SqlParameter[] param =
        //    //{
        //    //    new SqlParameter("@DisplayLength", length),
        //    //    new SqlParameter("@DisplayStart", start),
        //    //    new SqlParameter("@SortCol", sortColumn),
        //    //    new SqlParameter("@SortDir", sortColumnDir),
        //    //    new SqlParameter("@SEARCH", DBNull.Value),
        //    //    new SqlParameter("@IdDataPool", IdDataPool)
        //    //};

        //    //if (searchValue != "")
        //    //    param[4].Value = searchValue;
        //    //List<MassaTesteVO> MassaTesteVOs = new List<MassaTesteVO>();
        //    List<TestData> listaMassasTestes = new List<TestData>();
        //    MassaTesteVOs.AddRange(db.TestData.Where(x => x.IdDataPool == IdDataPool).ToList());

        //    //MassaTesteVOs = db.Database.SqlQuery<MassaTesteVO>(
        //    //        "EXEC PR_LISTAR_MASSA_TESTE @DisplayLength, @DisplayStart, @SortCol, @SortDir, @SEARCH, @IdDataPool ", param).ToList();

        //    ViewBag.ListaMassaTeste = listaMassasTestes.OrderBy(s => s.Descricao);

        //    if (listaMassasTestes.Count() != 0)
        //        TotalRows = listaMassasTestes.Count;

        //    return Json(new { draw = draw, recordsFiltered = TotalRows, recordsTotal = TotalRows, data = MassaTesteVOs }, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult Adicionar(string id)
        {

            ViewBag.ListaTDM = getListTDMByUser().Where(x => x.TdmPublico == false);
            ViewBag.ListaDemanda = db.Demanda.ToList();
            ViewBag.ListaAUT = db.AUT.ToList();
            ViewBag.listCondicaoScript = new List<CondicaoScript>();
            ViewBag.listScript = new List<Script>();
            ViewBag.IdCurrentTDM = id;
            return View();
        }

        [HttpPost]
        public ActionResult Remover(IList<string> ids)
        {
            var result = new JsonResult() { JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            for(int i = 0; i < ids.Count; i++)
            {
                try
                {
                    int idAtual = Int32.Parse(ids[i]);
                    DataPool datapool = db.DataPool.SingleOrDefault(a => a.Id == idAtual);
                    TDM TDM = db.TDM.SingleOrDefault(a => a.Id == datapool.IdTDM);

                    if (TDM.TdmPublico)
                    {
                        result.Data = new { Result = "Não é possível excluir um DATAPOOL PÚBLICO", Status = (int)WebExceptionStatus.UnknownError };
                    }
                    else
                    {
                        db.DataPool.Remove(datapool);
                        db.SaveChanges();

                        TempData["IdTDM"] = datapool.IdTDM;
                        result.Data = new { Result = "Datapool(s) removido(s) com sucesso.", Status = (int)WebExceptionStatus.Success };
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.ToString().Contains("FK_TestData_DataPool"))

                        result.Data = new { Result = "Esse registro contém dependência com outra entidade.", Status = (int)WebExceptionStatus.SendFailure };
                    else
                        result.Data = new { Result = ex.Message, Status = (int)WebExceptionStatus.UnknownError };
                }
            }            
            return result;
        }

        public ActionResult Editar(int id)
        {
            int qtdDisponivel;
            int qtdReservada;
            int qtdUtilizada;
            int qtdCadastrada;

            ViewBag.ListaAUT = db.AUT.ToList();
            ViewBag.ListaTDM = getListTDMByUser();
            ViewBag.ListaDemanda = db.Demanda.ToList();

            qtdDisponivel = (from qtd in db.TestData
                             where qtd.IdDataPool.Equals(id) && qtd.IdStatus.Equals((int)EnumStatusTestData.Disponivel)
                             select qtd).ToList().Count();

            qtdReservada = (from qtd in db.TestData
                            where qtd.IdDataPool.Equals(id) && qtd.IdStatus.Equals((int)EnumStatusTestData.Reservada)
                            select qtd).ToList().Count();

            qtdUtilizada = (from qtd in db.TestData
                            where qtd.IdDataPool.Equals(id) && qtd.IdStatus.Equals((int)EnumStatusTestData.Utilizada)
                            select qtd).ToList().Count();

            qtdCadastrada = (from qtd in db.TestData
                             where qtd.IdDataPool.Equals(id) && qtd.IdStatus.Equals((int)EnumStatusTestData.Cadastrada)
                             select qtd).ToList().Count();



            DataPool DataPoolSelecionado = db.DataPool.Where(x => x.Id == id).First();


            Script_CondicaoScript ScriptCondicaoScript = db.Script_CondicaoScript.FirstOrDefault(a => a.Id == DataPoolSelecionado.IdScript_CondicaoScript);
            ViewBag.Script = db.Script.Where(s => s.Id == ScriptCondicaoScript.IdScript).ToList();
            ViewBag.CondicaoScript = db.CondicaoScript.Where(s => s.Id == ScriptCondicaoScript.IdCondicaoScript).ToList(); ;

            ViewBag.qtdDisponivel = qtdDisponivel;
            ViewBag.qtdReservada = qtdReservada;
            ViewBag.qtdUtilizada = qtdUtilizada;
            ViewBag.Farol = 0;
            ViewBag.TdmPublico = DataPoolSelecionado.TDM.TdmPublico;


            if ((actualDate.AddDays(14) < DataPoolSelecionado.DataSolicitacao) || DataPoolSelecionado.DataTermino < actualDate.Date)
            {
                ViewBag.Farol = (int)EnumFarol.Cinza;
            }
            else if (qtdDisponivel < DataPoolSelecionado.QtdSolicitada)
            {
                ViewBag.Farol = (int)EnumFarol.Vermelho;

            }
            else if ((qtdDisponivel > DataPoolSelecionado.QtdSolicitada) && (qtdDisponivel < (DataPoolSelecionado.QtdSolicitada * 1.2)))
            {
                ViewBag.Farol = (int)EnumFarol.Amarelo;

            }
            else
            {
                ViewBag.Farol = (int)EnumFarol.Verde;
            }

            DataPool data = new DataPool();
            data = db.DataPool.FirstOrDefault(a => a.Id == id);

            DataPoolVO dataVO = new Models.VOs.DataPoolVO(data);
            dataVO.DataSolicitacao = data.DataSolicitacao;
            dataVO.Demanda = data.Demanda;
            dataVO.AUT = data.AUT;
            dataVO.DescricaoDataPool = data.Descricao;
            dataVO.Id = data.Id;
            dataVO.IdAut = data.IdAut;
            dataVO.IdDemanda = data.IdDemanda;
            dataVO.IdTDM = data.IdTDM;
            dataVO.QtdSolicitada = data.QtdSolicitada;
            dataVO.Observacao = data.Observacao;

            return View(dataVO);
        }

        public ActionResult SalvarEdicao(DataPool objeto)
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

            TempData["IdTDM"] = objeto.IdTDM;
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Salvar(DataPool objeto, bool editar = false)
        {
            var valor = Request.Form["MapClicks"];

            try
            {
                objeto.DataSolicitacao = DateTime.Now;
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
                    int idScript = Int32.Parse(Request.Form.Get("listScript"));
                    int idCondicaoScript;
                    Script_CondicaoScript script_CondicaoScript;

                    //Verifica se o DataPool tem Condição scrípt
                    if (Request.Form.Get("listCondicaoScript") == null)
                    {
                        script_CondicaoScript = db.Script_CondicaoScript.Where(x => x.IdScript == idScript).Where(x => x.IdCondicaoScript == null).ToList().FirstOrDefault();
                    }
                    else
                    {
                        idCondicaoScript = Int32.Parse(Request.Form.Get("listCondicaoScript"));
                        script_CondicaoScript = db.Script_CondicaoScript.Where(x => x.IdScript == idScript).Where(x => x.IdCondicaoScript == idCondicaoScript).ToList().FirstOrDefault();
                    }


                    objeto.IdScript_CondicaoScript = script_CondicaoScript.Id;

                        


                    int? IdDemanda = objeto.IdDemanda == null && !Request.Form.Get("demandaDatapool").IsNullOrWhiteSpace() ? Int32.Parse(Request.Form.Get("demandaDatapool")) : 0;
                    IdDemanda = IdDemanda == 0 ? null : IdDemanda;
                    //Validação se Já existe um DataPool com o mesmo Script e Condição
                    DataPool DataPool = db.DataPool.Where(x => x.IdTDM == objeto.IdTDM).Where(x => x.IdScript_CondicaoScript == objeto.IdScript_CondicaoScript).Where(x => x.IdDemanda == IdDemanda).FirstOrDefault();

                    if (DataPool == null)
                    {


                        //Atualizando os atributos dos objetos
                        objeto.IdAut = Int32.Parse(Request.Form.Get("sistemaDatapool"));
                        objeto.Descricao = Request.Form.Get("DescricaoDataPool");
                        objeto.QtdSolicitada = Int32.Parse(Request.Form.Get("QtdSolicitada"));
                        objeto.DataSolicitacao = (DateTime)Convert.ToDateTime(Request.Form.Get("DataSolicitacao"));
                        objeto.Observacao = Request.Form.Get("Observacao");
                        if (Request.Form.Get("demandaDatapool") != "")
                            objeto.IdDemanda = Int32.Parse(Request.Form.Get("demandaDatapool"));
                        else
                            objeto.IdDemanda = null;
                        objeto.IdScript_CondicaoScript = script_CondicaoScript.Id;

                        // anexar objeto ao contexto
                        db.DataPool.Attach(objeto);

                        //Prepara a entidade para uma Edição
                        db.Entry(objeto).State = System.Data.Entity.EntityState.Modified;

                        // informa que o obejto será modificado
                        db.SaveChanges();

                        //Retorna uma mensagem de sucesso
                        this.FlashSuccess("DataPool editado com Sucesso.");

                        TempData["IdTDM"] = objeto.IdTDM;
                    }
                    else
                    {
                        this.FlashWarning("O DataPool não pôde ser incluido pois existe um já cadastrado com a mesma condição, script e demanda.");
                    }
                }
                else
                {
                    int IdScript = Int32.Parse(Request.Form.Get("listScript"));
                    int IdCondicaoScript;
                    Script_CondicaoScript Script_CondicaoScript = new Script_CondicaoScript();

                    if (Request.Form.Get("listCondicaoScript") == null)
                    {
                        Script_CondicaoScript = db.Script_CondicaoScript.Where(x => x.IdScript == IdScript).Where(x => x.IdCondicaoScript == null).ToList().FirstOrDefault();
                    }
                    else
                    {
                        IdCondicaoScript = Int32.Parse(Request.Form.Get("listCondicaoScript"));
                        Script_CondicaoScript = db.Script_CondicaoScript.Where(x => x.IdScript == IdScript).Where(x => x.IdCondicaoScript == IdCondicaoScript).ToList().FirstOrDefault();
                    }

                    objeto.IdScript_CondicaoScript = Script_CondicaoScript.Id;

                    objeto.IdTDM = Int32.Parse(Request.Form.Get("projetoTDM"));
                    int? IdDemanda = objeto.IdDemanda == null && !Request.Form.Get("demandaDatapool").IsNullOrWhiteSpace() ? Int32.Parse(Request.Form.Get("demandaDatapool")) : 0;
                    IdDemanda = IdDemanda == 0 ? null : IdDemanda;
                    //Validação se Já existe um DataPool com o mesmo Script e Condição
                    DataPool DataPool = db.DataPool.Where(x => x.IdTDM == objeto.IdTDM).Where(x => x.IdScript_CondicaoScript == objeto.IdScript_CondicaoScript).Where(x => x.IdDemanda == IdDemanda).FirstOrDefault();

                    if (DataPool == null)
                    {
                        objeto.IdAut = Int32.Parse(Request.Form.Get("sistemaDatapool"));
                        if (objeto.IdDemanda == null && !Request.Form.Get("demandaDatapool").IsNullOrWhiteSpace())
                        {
                            objeto.IdDemanda = Int32.Parse(Request.Form.Get("demandaDatapool"));
                        }
                        objeto.DataSolicitacao = (DateTime)Convert.ToDateTime(Request.Form.Get("DataSolicitacao"));
                        objeto.DataTermino = (DateTime)Convert.ToDateTime(Request.Form.Get("DataTermino"));
                        objeto.Observacao = Request.Form.Get("Observacao");
                        db.DataPool.Add(objeto);
                        db.SaveChanges();

                        this.FlashSuccess("DataPool adicionado com Sucesso.");
                        TempData["IdTDM"] = objeto.IdTDM;
                    }
                    else
                    {
                        this.FlashWarning("O DataPool não pôde ser incluido pois existe um já cadastrado com a mesma condição, script e demanda.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.ToString().Contains("AK_DataPool_Descricao"))
                    this.FlashError("Já existe um DataPool com essa descrição.");
                else
                    //throw new Exception(ex.Message);
                    this.FlashError(ex.Message);

            }

            return RedirectToAction("Index");
        }

        private void getMinhasDemandas()
        {

            Usuario usuarioLogado = (Usuario)Session["ObjUsuario"];
            List<Demanda> minhasDemandas = new List<Demanda>();
            List<DataPool> listDatapool = new List<DataPool>();
            List<TDM_Usuario> listTDMByUser = new List<TDM_Usuario>();
            listTDMByUser.AddRange(db.TDM_Usuario.Where(x => x.IdUsuario == usuarioLogado.Id).ToList());

            foreach (TDM_Usuario tdmUsuario in listTDMByUser)
            {
                listDatapool.AddRange(db.DataPool.Where(y => y.IdTDM == tdmUsuario.IdTDM).ToList());

                foreach (DataPool dataPool in listDatapool)
                {
                    if (!minhasDemandas.Contains(dataPool.Demanda))
                    {
                        minhasDemandas.Add(dataPool.Demanda);
                    }
                }

                listDatapool = new List<DataPool>();

            }

            minhasDemandas.OrderBy(o => o.Descricao);
            //atribui a lista de demandas a session 'minhasDemandas'
            Session["minhasDemandas"] = minhasDemandas;
        }


        public JsonResult CarregaDemanda(string id)
        {

            int idDemanda = int.Parse(id);

            List<DataPool> dataPools = new List<DataPool>();
            dataPools.AddRange(db.DataPool.Where(x => x.IdDemanda == idDemanda).ToList());
            int? idDataPool = dataPools.First().Id;
            return (Json(new { id = idDataPool }, JsonRequestBehavior.AllowGet));

        }

        public JsonResult getListCondicaoScriptByScript(string id)
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

        public JsonResult getListScriptsBySystem(string id)
        {
            int IdSystem = Int32.Parse(id);

            List<Script> Scripts = db.Script.Where(x => x.IdAUT == IdSystem).ToList();

            ViewBag.listScript = Scripts;

            Dictionary<string, string> ListaScripts = new Dictionary<string, string>();

            foreach (var item in Scripts)
            {
                ListaScripts.Add(item.Id + "", item.Descricao);
            }
            string json = JsonConvert.SerializeObject(ListaScripts, Formatting.Indented);


            return Json(json, JsonRequestBehavior.AllowGet);
        }

        private List<TDM> getListTDMByUser()
        {
            //PEGA O OBJETO COM O USUÁRIO QUE ESTÁ LOGADO
            Usuario user = (Usuario)Session["ObjUsuario"];

            List<TDM_Usuario> listTDMByUser = new List<TDM_Usuario>();
            listTDMByUser.AddRange(db.TDM_Usuario.Where(x => x.IdUsuario == user.Id).ToList());

            //CRIADA NOVA LISTA DO OBJETO TDM
            List<TDM> listaTDM = new List<TDM>();

            foreach (TDM_Usuario tdmUsuario in listTDMByUser)
            {
                listaTDM.AddRange(db.TDM.Where(x => x.Id == tdmUsuario.IdTDM).ToList());

            }


            return listaTDM;
        }

        public JsonResult GetDadosModalPlay(string id)

        {
            DataPool datapool = db.DataPool.Find(Int32.Parse(id));

            Script_CondicaoScript script_CondicaoScript = db.Script_CondicaoScript.Find(datapool.IdScript_CondicaoScript);

            // Recuperando todos os ambientes execução e virtual possível para o Script+Condicao da tela
            var QueryAmbientes =
               (from av in db.AmbienteVirtual
                join sca in db.Script_CondicaoScript_Ambiente on av.Id equals sca.IdAmbienteVirtual
                join aexec in db.AmbienteExecucao on sca.IdAmbienteExecucao equals aexec.Id
                where sca.IdScript_CondicaoScript == script_CondicaoScript.Id // pegar id da tela
                select new AmbienteExecucao_Popup
                {
                    IdAmbienteVirtual = av.Id,
                    DescricaoAmbienteVirtual = av.Descricao,
                    IdAmbienteExecucao = aexec.Id,
                    DescricaoAmbienteExecucao = aexec.Descricao,
                    Disponivel = true

                }).ToList().Distinct();

            // Recuperando todos os ambientes execução que estão em uso
            var QueryAmbienteVirtualDisponivel =
                (from exec in db.Execucao
                 where exec.SituacaoAmbiente == (int)Enumerators.EnumSituacaoAmbiente.EmUso
                 select exec.Script_CondicaoScript_Ambiente.AmbienteVirtual.Id).ToList();

            // Percorrendo todos os ambientes execução e virtual possíveis e atualizando o status (disponível)
            foreach (var item in QueryAmbientes)
            {
                if (QueryAmbienteVirtualDisponivel.Contains(item.IdAmbienteVirtual))
                    item.Disponivel = false;
            }

            // Recuperando todos os ambientes que possuem status disponível
            ViewBag.ListaAmbienteVirt = QueryAmbientes.Where(i => i.Disponivel == true).Select(i => new { i.IdAmbienteVirtual, i.DescricaoAmbienteVirtual }).ToList();
            ViewBag.ListaAmbienteExec = QueryAmbientes.Where(i => i.Disponivel == true).Select(i => new { i.IdAmbienteExecucao, i.DescricaoAmbienteExecucao }).ToList();

            //string json = JsonConvert.SerializeObject(ViewBag.ListaAmbienteExec, Formatting.Indented);

            string jsonAmbExec = JsonConvert.SerializeObject(ViewBag.ListaAmbienteExec, Formatting.Indented);
            string jsonAmbVirtual = JsonConvert.SerializeObject(ViewBag.ListaAmbienteVirt, Formatting.Indented);

            var ambientes = new { ambexec = jsonAmbExec, ambvirtu = jsonAmbVirtual };

            return Json(ambientes, JsonRequestBehavior.AllowGet);
        }



        public ActionResult Play(string id, string idFaseTeste, string idMaquinaVirtual, string opcaoTelegram, string idAmbienteExecucao)
        {
            try
            {
                string mensagem = "";
                var testDatas = new List<int>();
                int IdDatapool = Int32.Parse(id);
                List<ListaTestDatas> listaTestDatas =
                    (from dp in db.DataPool
                     join td in db.TestData on dp.Id equals td.IdDataPool
                     where dp.Id == IdDatapool
                     select new ListaTestDatas
                     {
                         IdDatapool = dp.Id,
                         IdTestData = td.Id
                     }).ToList();

                for (int i = 0; i < listaTestDatas.Count; i++)
                {
                    int idTestDataAtual = listaTestDatas[i].IdTestData;
                    List<ParametrosValores> listaParametrosObrigatorios =
                    (from pv in db.ParametroValor
                     join ps in db.ParametroScript on pv.IdParametroScript equals ps.Id
                     join p in db.Parametro on ps.IdParametro equals p.Id
                     where pv.IdTestData == idTestDataAtual
                     select new ParametrosValores
                     {
                         IdTestData = IdDatapool,
                         IdDatapool = idTestDataAtual,
                         Descricao = p.Descricao,
                         Valor = pv.Valor,
                         Obrigatorio = ps.Obrigatorio
                     }).ToList();

                    for (int w = 0; w < listaParametrosObrigatorios.Count; w++)
                    {
                        if (listaParametrosObrigatorios[w].Valor == "")
                        {
                            if (!testDatas.Contains(listaTestDatas[i].IdTestData))
                            {
                                testDatas.Add(listaTestDatas[i].IdTestData);
                            }
                        }
                    }                    
                }

                if (testDatas.Count > 0)
                {
                    string combindedString = string.Join(",", testDatas.ToArray());
                    mensagem = "O(s) TestData(s): " + combindedString + " possui(em) parâmetro(s) obrigatório(s) - destacados em vermelho - que não foram preenchidos";
                }
                else
                {
                    // Utilizando o Datapool da tela, substituir a query do script e salvar os dados na tabela de execução (Controle_Ambiente)
                    ReplaceQuery(ObtemIdTestData(Int32.Parse(id)), Int32.Parse(idFaseTeste), Int32.Parse(idMaquinaVirtual), Int32.Parse(idAmbienteExecucao)); // enviar o Datapool da tela

                    #region Realizar execução através de requisição Jenkins
                    string pAginaDoJob = null;
                    int idAmbv = Int32.Parse(idMaquinaVirtual);
                    AmbienteVirtual ambv = db.AmbienteVirtual.Where(x => x.Id == idAmbv).FirstOrDefault();

                    if (ambv.Descricao == "VDI01")
                    {
                        pAginaDoJob = "http://10.43.6.160:8080/buildByToken/build?job=VDI141&?token=tdm141";
                    }
                    else if (ambv.Descricao == "VDI02")
                    {
                        pAginaDoJob = "http://10.43.6.160:8080/buildByToken/build?job=VDI219&?token=tdm219";
                    }
                    else
                    {
                        this.FlashError("Não foi possível definir o Job do Jenkins.");
                    }

                    WebRequest wrIniciaJob;

                    wrIniciaJob = WebRequest.Create(pAginaDoJob);

                    wrIniciaJob.Method = "POST";

                    WebResponse response = wrIniciaJob.GetResponse();

                    response.Close();

                    #endregion

                    mensagem = "Execução iniciada com sucesso!";
                }

                return Json(new { Data = mensagem }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        private List<int> ObtemIdTestData(int id_datapool)
        {

            // Instancia a Entity
            DbEntities db = new DbEntities();

            // Query para buscar o id do TestData,passando o id do Datapool que será executado.
            var tdQuery =
            (from td1 in db.TestData
             join dtp in db.DataPool on td1.IdDataPool equals dtp.Id
             where dtp.Id == id_datapool && td1.IdStatus == 1
             select td1.Id).ToList();

            return tdQuery;
        }



        private void ReplaceQuery(List<int> ids, int idFaseTeste, int idMaquinaVirtual, int idAmbienteExecucao)
        {
            DbEntities db = new DbEntities();
            // validar ambiente disponível e setar a flag em uso antes de inserir a query

            //recuperando objeto testdata, para ter recuperar o IdScript_CondicaoScript
            TestData testData = db.TestData.Find(ids[0]);

            Script_CondicaoScript script_CondicaoScript = db.Script_CondicaoScript.Where(x => x.Id == testData.IdScript_CondicaoScript).FirstOrDefault();

            string query = script_CondicaoScript.QueryTosca;

            string testdataList = "";
            //construindo a lista de ids de TestData
            for (int i = 0; i < ids.Count(); i++)
            {
                if (i == 0)
                {
                    testdataList = "" + ids[i];
                }
                else
                {
                    testdataList += "," + ids[i];
                }
            }

            query = query.Replace("ptdTosca", testdataList);

            foreach (var item in ids)
            {
                Usuario user = (Usuario)Session["ObjUsuario"];
                Execucao exec = new Execucao();
                Script_CondicaoScript_Ambiente script_CondicaoScript_Ambiente = db.Script_CondicaoScript_Ambiente.Where(x => x.IdScript_CondicaoScript == testData.IdScript_CondicaoScript).Where(x => x.IdAmbienteVirtual == idMaquinaVirtual).FirstOrDefault();
                exec.IdScript_CondicaoScript_Ambiente = script_CondicaoScript_Ambiente.Id;
                exec.IdTipoFaseTeste = idFaseTeste; // pegar via campo popup modal play
                exec.IdStatusExecucao = (int)Enumerators.EnumStatusExecucao.AguardandoProcessamento;
                exec.Usuario = user.Id.ToString();
                exec.IdTestData = item; // pegar o id via tela
                exec.SituacaoAmbiente = (int)Enumerators.EnumSituacaoAmbiente.EmUso;
                exec.ToscaInput = query;
                db.Execucao.Add(exec);

                DbEntities db1 = new DbEntities();
                TestData td1 = db1.TestData.Where(x => x.Id == item).FirstOrDefault();
                td1.IdStatus = (int)Enumerators.EnumStatusTestData.EmGeracao;

                db1.TestData.Attach(td1);

                db1.Entry(td1).State = System.Data.Entity.EntityState.Modified;


                try
                {
                    db.SaveChanges();
                    db1.SaveChanges();
                }
                catch (Exception ex)
                {
                    this.FlashError(ex.Message);
                }

            }


        }


        public JsonResult CarregarParametros(string idScriptCondicaoScript, string idDataPool)
        {
            DbEntities db = new DbEntities();
            int scriptCondicaoScript = Int32.Parse(idScriptCondicaoScript);
            int dataPool = Int32.Parse(idDataPool);

            //busca a lista de parâmetros filtrando pelo tipo de parâmetro input e pelo idScriptCondicaoScript
            List<ParametroVO> listParametros = (from parametroScript in db.ParametroScript
                                                join parametro in db.Parametro on parametroScript.IdParametro equals parametro.Id
                                                where parametroScript.IdTipoParametro == (int)EnumTipoParametro.Input && parametroScript.IdScript_CondicaoScript == scriptCondicaoScript
                                                select new ParametroVO { Descricao = parametro.Descricao, IdParametroScript = parametroScript.Id, IdParametro = parametro.Id, Tipo = parametro.Tipo }).ToList();

            //realiza busca dos testdatas filtrando pelos datapool 
            List<TestDataVO> listTestData = (from testData in db.TestData
                                             where testData.IdDataPool == dataPool
                                             join status in db.Status on testData.IdStatus equals status.Id
                                             orderby testData.Id ascending
                                             select new TestDataVO { CasoTesteRelativo = testData.CasoTesteRelativo, Descricao = testData.Descricao, GeradoPor = testData.GeradoPor, DataGeracao = testData.DataGeracao, IdTestData = testData.Id, Observacao = testData.Observacao, Status = status.Descricao, IdStatus = testData.IdStatus, IdScriptCondicaoScript = testData.IdScript_CondicaoScript }).ToList();



            foreach (TestDataVO item in listTestData)
            {
                List<ParametroValorVO> listParametroValorVO = new List<ParametroValorVO>();
                List<ParametroValor> valores = db.ParametroValor.Where(x => x.IdTestData == item.IdTestData).ToList();

                List<ParametroScript> listParametroScript = db.ParametroScript.Where(x => x.IdScript_CondicaoScript == item.IdScriptCondicaoScript && x.IdTipoParametro == (int)EnumTipoParametro.Input).ToList();

                foreach (ParametroScript ps in listParametroScript)
                {
                    //realiza busca dos valores dos parâmetros associados ao testdata, caso não seja encontrado será adicionado a lista como vazio
                    ParametroValor pv = db.ParametroValor.Where(x => x.IdParametroScript == ps.Id && x.IdTestData == item.IdTestData).FirstOrDefault();
                    if (pv == null)
                    {
                        listParametroValorVO.Add(new ParametroValorVO { Valor = "", Id = 0, IdParametroScript = 0 });
                    }
                    else
                    {
                        listParametroValorVO.Add(new ParametroValorVO { Valor = pv.Valor, Id = pv.Id, IdParametroScript = pv.IdParametroScript });
                    }
                }

                item.valores = listParametroValorVO;
            }



            List<TestDataParametroVO> testDataParametroVO = new List<TestDataParametroVO>();
            testDataParametroVO.Add(new TestDataParametroVO { parametros = listParametros, testData = listTestData });

            string json = JsonConvert.SerializeObject(testDataParametroVO, Formatting.Indented);

            return (Json(json, JsonRequestBehavior.AllowGet));

        }

        public JsonResult atualizaParametroValor(string id, string valor, string idParametroScript, string idTestData, string idScriptCondicaoScript, string idParametro)
        {
            DbEntities db = new DbEntities();

            int idParametroValor = Int32.Parse(id);
            int parametroScriptId = Int32.Parse(idParametroScript);
            int testDataId = Int32.Parse(idTestData);
            int scriptCondicaoScriptId = Int32.Parse(idScriptCondicaoScript);
            int parametroId = Int32.Parse(idParametro);


            ParametroValor parametroValor = null;

            if (idParametroValor != 0)
            {

                parametroValor = new ParametroValor { Id = idParametroValor, IdParametroScript = parametroScriptId, Valor = valor, IdTestData = testDataId };

                db.ParametroValor.Attach(parametroValor);
                // informa que o obejto será modificado
                db.Entry(parametroValor).State = System.Data.Entity.EntityState.Modified;

                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine(dbEx.Message);
                }

            }
            else
            {
                ParametroScript parametroScript = db.ParametroScript.Where(x => x.IdScript_CondicaoScript == scriptCondicaoScriptId && x.IdParametro == parametroId).FirstOrDefault();
                TestData testData = db.TestData.Find(testDataId);
                parametroValor = new ParametroValor { IdParametroScript = parametroScript.Id, IdTestData = testDataId, Valor = valor, TestData = testData, ParametroScript = parametroScript };

                db.ParametroValor.Add(parametroValor);
                db.SaveChanges();
            }

            string json = JsonConvert.SerializeObject(new ParametroValor { Id = parametroValor.Id, Valor = parametroValor.Valor, IdTestData = parametroValor.IdTestData, IdParametroScript = parametroValor.IdParametroScript }, Formatting.Indented);


            return (Json(json, JsonRequestBehavior.AllowGet));
        }

        public bool Equals(DataPoolController other)
        {
            throw new NotImplementedException();
        }
    }

    internal class AmbienteExecucao_Popup
    {
        public string DescricaoAmbienteExecucao { get; set; }
        public string DescricaoAmbienteVirtual { get; set; }
        public bool Disponivel { get; set; }
        public int IdAmbienteExecucao { get; set; }
        public int IdAmbienteVirtual { get; set; }
    }

    internal class ListaTestDatas
    {
        public int IdDatapool { get; set; }
        public int IdTestData { get; set; }
    }

    internal class ParametrosValores
    {
        public int IdDatapool { get; set; }
        public int IdTestData { get; set; }

        public bool Obrigatorio { get; set; }

        public string Descricao { get; set; }

        public string Valor { get; set; }
    }

}
