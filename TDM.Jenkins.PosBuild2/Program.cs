using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TDM.Jenkins.PosBuild;
using LaefazWeb.Models;
using System.Text.RegularExpressions;
using LaefazWeb.Enumerators;
using System.IO.Compression;
using System.Security.Permissions;
using System.Threading;
using System.Globalization;

namespace TDM.Jenkins.PosBuild2
{
    class Program
    {

        private static string Canal = "-1001152418569";
        private static string TokenBotTelegram = "519409625:AAEHbw4N3-y0BHHpTp_mIbXp6ygzNogfTnc";
        private static string Message = "";
        private static string Ip = "";
        private static DadosExecucao CarregaDadosExecucao;
        //Entidade de banco de dados
        private static DbEntities db = new DbEntities();
        private static List<TestData> TestDataGeneratedSuccessfully = new List<TestData>();
        private static List<TestData> TestDataGeneratedFailed = new List<TestData>();
        private static List<TestData> TestDataNoRun = new List<TestData>();
        private static List<TestData> TestDataGeneratedSolicited;

        static void Main(string[] args)
        {
            try
            {
                //Exclusão do arquivo de query da execução
                string dir = "C:\\Tosca_Projects\\Tosca_Templates\\queryes\\";

                foreach (string f in Directory.EnumerateFiles(dir, "*.txt"))
                {
                    File.Delete(f);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Não foi possível apagar os arquivos de query da execução.");
                Console.WriteLine("Erro: "+ ex.Message);
            }

            //Console Log
            Console.WriteLine("[INFO] - Criada a instância do DbEntities.");

            //Console Log
            Console.WriteLine("[INFO] - Capturando Ip local da máquina de execução.");
            Ip = GetIpLocal();

            //Console Log
            Console.WriteLine("[INFO] - Ip local da máquina recuperado com sucesso.");
            Console.WriteLine("[INFO] - Processo de Pós Build 2 Iniciado na máquina " + Ip + ".");

            //Recebendo string com diretório completo do arquivo de Report
            string path = "C:\\Tosca_Projects\\Tosca_Workspaces\\TDM\\remote_exec\\Reports\\report.txt";

            //Pasta compartilhada da VDI que corresponde a pasta C:\\EvidenceTDM\\ da VDI da aplicação
            string PathSaveEvidence = "U:\\";
            int? IdDataPool = null;

            try
            {
                // Console Log
                Console.WriteLine("[INFO] - Entrada no Try Catch.");

                // Console Log
                Console.WriteLine("[INFO] - Iniciando a leitura do arquivo do log report.");
                //Ler todas as linhas do arquivo e preenche um array

                string[] lines = File.ReadAllLines(@path);

                // Console Log
                Console.WriteLine("[INFO] - O arquivo de report foi acessado com sucesso.");

                //Criação das listas para realizar as atualizações do pos Build 2
                List<string> TestDataReport = new List<string>();
                List<string> ExecutionReport = new List<string>();

                // Console Log
                Console.WriteLine("[INFO] - Entrada no report do Tosca.");

                //Capturando todas os TestData da execução e o diretório de suas respectivas evidências
                Array.ForEach(lines, element =>
                {
                    // Console Log
                    Console.WriteLine("[INFO] - " + element);

                    if (element.Contains("ID_TEST_DATA"))
                        TestDataReport.Add(element);
                    else if (element.Contains("executionFolder"))
                        ExecutionReport.Add(element);
                });

                // Console Log
                Console.WriteLine("[INFO] - Foram encontrados no report registro de " + TestDataReport.Count() + " execuções.");

                //Valida que foram retornadas a quantidade correta de path de evidências para cada TestData
                if (TestDataReport.Count() == ExecutionReport.Count())
                {
                    // Console Log
                    Console.WriteLine("[INFO] - Entrada no loop de execuções.");

                    //Iterando pelos reports do TestData
                    for (int i = 0; i < TestDataReport.Count(); i++)
                    {
                        string @CaminhoEvidence = "http://10.43.6.160:8081/PortalTDM/Evidencias/";
                        //Regex para pegar as informações das linhas
                        string delimiter = "[^\"|,]+";
                        MatchCollection TestDataAttributes = Regex.Matches(TestDataReport[i], delimiter);
                        MatchCollection ExecutionAttributes = Regex.Matches(ExecutionReport[i], delimiter);

                        // Console Log
                        Console.WriteLine("[INFO] - Os dados do log report foram interpretados conforme esperado.");

                        int IdTestData = Int32.Parse(TestDataAttributes[1].Value);
                        string StatusTestData = TestDataAttributes[2].Value.Equals("Passed") ? "DISPONÍVEL" : "ERRO";
                        string StatusExecucao = TestDataAttributes[2].Value.Equals("Passed") ? "Sucesso" : "Falha";
                        string Evidencia = ExecutionAttributes[1].Value;
                        DateTime ExecutionDate = DateTime.Now;

                        // Console Log
                        Console.WriteLine("[INFO] - Iniciando carregamento das entidades do banco.");

                        //Carregando entidades do model
                        Execucao Execucao = db.Execucao.Where(x => x.IdTestData == IdTestData).Where(x => x.SituacaoAmbiente == (int)EnumSituacaoAmbiente.EmUso).FirstOrDefault();
                        
                        // Console Log
                        Console.WriteLine("[INFO] - Entidade Execução carregada com sucesso.");

                        TestData TestData = db.TestData.FirstOrDefault(x => x.Id == IdTestData);

                        // Pegando o número de execuções agendadas
                        if (TestDataGeneratedSolicited == null)
                        {
                            DataPool dt = db.DataPool.Where(x => x.Id == TestData.IdDataPool).FirstOrDefault();
                            TestDataGeneratedSolicited = db.TestData.Where(x => x.IdDataPool == dt.Id).Where(x=>x.IdStatus==(int)EnumStatusTestData.EmGeracao).ToList();
                        }


                        // Console Log
                        Console.WriteLine("[INFO] - Entidade TestData carregada com sucesso.");

                        IdDataPool = IdDataPool == null ? TestData.IdDataPool : IdDataPool;

                        StatusExecucao statusExecucao = db.StatusExecucao.FirstOrDefault(x => x.Descricao == StatusExecucao);
                        // Console Log
                        Console.WriteLine("[INFO] - Entidade StatusExecução carregada com sucesso.");

                        Status statusTestData = db.Status.FirstOrDefault(x => x.Descricao == StatusTestData);
                        // Console Log
                        Console.WriteLine("[INFO] - Entidade Status carregada com sucesso.");

                        Console.WriteLine("[INFO] - AUT = " + TestData.DataPool.AUT.Descricao);
                        Console.WriteLine("[INFO] - Demanda = " + TestData.DataPool.Demanda.Descricao);
                        Console.WriteLine("[INFO] - TipoFaseTeste = " + Execucao.TipoFaseTeste.Descricao);

                        string AUT = TestData.DataPool.AUT.Descricao;
                        string Demanda = TestData.DataPool.Demanda.Descricao;
                        string TipoFaseTeste = Execucao.TipoFaseTeste.Descricao;
                        // Console Log
                        Console.WriteLine("[INFO] - Informações de AUT, Demanda e TipoFaseTeste carregados com sucesso.");

                        //STC-ETS-PRJ00007149_ENT00003722-06_04_2018_10_47
                        string ZipExtension = ".ZIP";
                        string FolderEvidenceName = AUT + "-E" + TipoFaseTeste + "-" + Demanda + "-" + String.Format("{0:dd_MM_yyyy_hh_mm_ss}", ExecutionDate);
                        string DirectoryTemp = Evidencia + "\\" + FolderEvidenceName;

                        // Console Log
                        Console.WriteLine("[INFO] - Iniciando carregamento dos arquivos da pasta de evidências de testData.");

                        //Carregando os arquivos da pasta de evidência do teste Data
                        string[] files = System.IO.Directory.GetFiles(Evidencia);

                        // Console Log
                        Console.WriteLine("[INFO] - Arquivos da pasta de TestData carregados com sucesso.");

                        //Criando um diretório temporario para colocar todos os arquivos e zipar
                        Directory.CreateDirectory(DirectoryTemp);

                        //Copiando os arquivos da pasta de evidencia para o diretório temporario
                        Array.ForEach(files, file =>
                        {
                            string fileName = System.IO.Path.GetFileName(file);
                            string destFile = System.IO.Path.Combine(DirectoryTemp, fileName);
                            System.IO.File.Copy(file, destFile, true);
                        });

                        // Console Log
                        Console.WriteLine("[INFO] - Arquivos copiados com sucesso para a pasta temp.");

                        //Zipando a pasta com os arquivos de evidência do TestData
                        ZipFile.CreateFromDirectory(DirectoryTemp, DirectoryTemp + ZipExtension);

                        // Console Log
                        Console.WriteLine("[INFO] - Arquivos zipado com sucesso para a pasta temp.");

                        //Monta o diretório com a data e hora da execução
                        string Now = String.Format("{0:dd_MM_yyyy}", ExecutionDate);
                        // Console Log
                        Console.WriteLine("[INFO] - Entrada para verificação se a pasta do dia já existe.");

                        string folderTdmEvidencesRoot = System.IO.Directory.GetDirectories(PathSaveEvidence, Now).FirstOrDefault();

                        //Cria o caminho final da evidência
                        string folderTdmEvidences = PathSaveEvidence + "\\" + Now + "\\" + FolderEvidenceName + ZipExtension;

                        //Verifica se já existe pasta de excução do dia 
                        //Caso não exista cria uma
                        if (folderTdmEvidencesRoot == null)
                            Directory.CreateDirectory(PathSaveEvidence + "\\" + Now);

                        //Copiando a evidência para a pasta de Evidencias do portal
                        System.IO.File.Copy(DirectoryTemp + ZipExtension, folderTdmEvidences, true);
                        // Console Log
                        Console.WriteLine("[INFO] - Copia do Zip realizada com sucesso para a pasta C:\\Evidences\\");

                        //Atualizando o registro de TestData
                        CaminhoEvidence += "/" + Now + "/" + FolderEvidenceName + ZipExtension;
                        TestData.IdStatus = statusTestData.Id;
                        TestData.CaminhoEvidencia = CaminhoEvidence;
                        TestData.TerminoExecucao = DateTime.Now;
                        // anexar objeto ao contexto
                        db.TestData.Attach(TestData);
                        //Prepara a entidade para uma Edição
                        db.Entry(TestData).State = System.Data.Entity.EntityState.Modified;
                        // informa que o obejto será modificado
                        db.SaveChanges();

                        // Console Log
                        Console.WriteLine("[INFO] - Atualização do objeto TestData realizado com sucesso!");

                        //Atualizando o registro de Execução
                        Execucao.IdStatusExecucao = statusExecucao.Id;
                        Execucao.SituacaoAmbiente = (int)EnumSituacaoAmbiente.Disponivel;
                        Execucao.ToscaOutput = string.Join("", lines);
                        // anexar objeto ao contexto
                        db.Execucao.Attach(Execucao);
                        //Prepara a entidade para uma Edição
                        db.Entry(Execucao).State = System.Data.Entity.EntityState.Modified;
                        // informa que o obejto será modificado
                        db.SaveChanges();

                        // Console Log
                        Console.WriteLine("[INFO] - Atualização do objeto Execução realizado com sucesso!");

                        //Preenchendo a lista de TestData para relatório
                        if (TestData.Status.Descricao == "DISPONÍVEL")
                            TestDataGeneratedSuccessfully.Add(TestData);
                        else
                            TestDataGeneratedFailed.Add(TestData);

                        //Sleep para garantir que as evidências não fiquem com Data e hora iguais
                        Thread.Sleep(1000);
                    }

                    // Console Log
                    Console.WriteLine("[INFO] - Verificando se foi encontrado o Id do DataPool da execução.");

                    //Verifica se consegui capturar o Id do DataPool
                    if (IdDataPool == null)
                    {
                        throw new Exception("[ERROR] - Não foi possível identificar o Id do DataPool para os TestData executados.");
                    }
                    else
                    {
                        // Console Log
                        Console.WriteLine("[INFO] - Id do Datapool "+ IdDataPool + " da execução foi encontrado com sucesso.");

                        //Carrega todas as massas ficaram com o status de EM GERAÇÃO para CADASTRADA
                        List<TestData> TestData = db.TestData.Where(x => x.IdDataPool == IdDataPool).Where(x => x.Status.Id == (int)EnumStatusTestData.EmGeracao).ToList();
                        //Itera pela lista de TestData alterando os status

                        //Carrega TestData que não foram executados
                        TestDataNoRun = db.TestData.Where(x => x.IdDataPool == IdDataPool).Where(x => x.Status.Id == (int)EnumStatusTestData.EmGeracao).ToList();

                        TestData.ForEach(element =>
                        {
                            Console.WriteLine("Entrada no loop de atualização das massas com status EM GERAÇÃO.");
                            element.IdStatus = 1;
                            db.TestData.Attach(element);
                            //Prepara a entidade para uma Edição
                            db.Entry(element).State = System.Data.Entity.EntityState.Modified;
                            // informa que o obejto será modificado
                            db.SaveChanges();

                            Execucao Execucao = db.Execucao.Where(x => x.Id == element.IdExecucao).FirstOrDefault();
                            Execucao.SituacaoAmbiente = (int)EnumSituacaoAmbiente.Disponivel;

                            db.Execucao.Attach(Execucao);
                            //Prepara a entidade para uma Edição
                            db.Entry(Execucao).State = System.Data.Entity.EntityState.Modified;
                            // informa que o obejto será modificado
                            db.SaveChanges();
                        });

                        //Status da execução
                        Message = GetExecutionSuccessMessage(IdDataPool);
                        //"A execução do script " + dataPool.Script_CondicaoScript.Script.Descricao + " foi finalizada com sucesso";

                    }
                }
                else
                {
                    // Console Log
                    Console.WriteLine("[INFO] - Houve um erro na captura de parâmetros do relatório gerado pelo Tosca. Não foi possível encontrar as pastas de executionFoder para todos os testData.");
                    throw new Exception("Houve um erro na captura de parâmetros do relatório gerado pelo Tosca. Não foi possível encontrar as pastas de executionFoder para todos os testData.");
                }
            }

            catch (Exception ex)
            {

                Console.WriteLine("\tMessage: " + ex.Message);
                Console.WriteLine("\tInnerException: " + ex.InnerException);

                CarregaDadosExecucao = (
                  from exec in db.Execucao
                  join sca in db.Script_CondicaoScript_Ambiente on exec.IdScript_CondicaoScript_Ambiente equals sca.Id
                  join ambv in db.AmbienteVirtual on sca.IdAmbienteVirtual equals ambv.Id
                  join td in db.TestData on exec.IdTestData equals td.Id
                  join dtp in db.DataPool on td.IdDataPool equals dtp.Id
                  join dmd in db.Demanda on dtp.IdDemanda equals dmd.Id
                  join scs in db.Script_CondicaoScript on sca.IdScript_CondicaoScript equals scs.Id
                  join scr in db.Script on scs.IdScript equals scr.Id
                  where exec.SituacaoAmbiente == (int)EnumSituacaoAmbiente.EmUso && ambv.IP == Ip

                  select new DadosExecucao
                  {
                      IdTestData = exec.IdTestData
                      ,
                      IdExecucao = exec.Id
                      ,
                      DatapoolName = scr.Descricao
                      ,
                      DemandaName = dmd.Descricao

                  }).FirstOrDefault();


                if (ex.Message.Contains("Could not find file"))
                {
                    Message = "Não foi possível encontrar o arquivo de report da execução para realizar o processo de finalização da execução";
                }
                else
                {
                    Message = "Ocorreu um erro na execução do Datapool: " + CarregaDadosExecucao.DatapoolName + ", referente ao " + CarregaDadosExecucao.DemandaName + ". Por favor contactar o administrador do sistema.";
                }

                
                //Eviando a Mensagem para o Telegran
                //SMS.Enviar(message, Canal, TokenBotTelegram);
            }
            finally
            {

                //Eviando a Mensagem para o Telegran
                SMS.Enviar(Message, Canal, TokenBotTelegram);
                Execucao exec = db.Execucao.FirstOrDefault(x => x.Id == CarregaDadosExecucao.IdExecucao);

                //Liberando as massas que ficaram com o status em Geração
                if (IdDataPool == null)
                {

                    IdDataPool = db.TestData.Where(x => x.Id == exec.IdTestData).FirstOrDefault().IdDataPool;
                    List<TestData> TestDataList = db.TestData.Where(x=> x.IdDataPool == IdDataPool).Where(x=>x.IdStatus == (int)EnumStatusTestData.EmGeracao).ToList();

                    TestDataList.ForEach(element => {
                        element.IdStatus = (int)EnumStatusTestData.Cadastrada;
                        // anexar objeto ao contexto
                        db.TestData.Attach(element);
                        //Prepara a entidade para uma Edição
                        db.Entry(element).State = System.Data.Entity.EntityState.Modified;
                        // informa que o obejto será modificado
                        db.SaveChanges();

                        Execucao Execucao = db.Execucao.Where(x=>x.Id == element.IdExecucao).FirstOrDefault();
                        //Liberando o ambiente
                        Execucao.SituacaoAmbiente = (int)EnumSituacaoAmbiente.Disponivel;
                        Execucao.IdStatusExecucao = element.IdStatus == (int)EnumStatusTestData.Falha ? (int)EnumStatusExecucao.Falha : (int)EnumStatusExecucao.Sucesso;
                        db.Execucao.Attach(Execucao);
                        //Prepara a entidade para uma Edição
                        db.Entry(Execucao).State = System.Data.Entity.EntityState.Modified;
                        // informa que o obejto será modificado
                        db.SaveChanges();

                    });

                   
                }

               

            
            }

        }

        private static string GetExecutionSuccessMessage(int? idDataPool)
        {
            DataPool DataPool = db.DataPool.FirstOrDefault(x => x.Id == idDataPool);
            List<TestData> TestData = db.TestData.Where(x => x.IdDataPool == idDataPool).ToList();

            //Pegando dados do relatório
            string Demanda = DataPool.Demanda == null ? "" : DataPool.Demanda.Descricao;
            string Script = DataPool.Script_CondicaoScript.Script.Descricao;
            //string Data = String.Format("{0:dd MMMM yyyy hh:mm:ss}", DateTime.Now);
            string Data = DateTime.Now.ToString(@"dd/MM/yyyy HH:mm:ss", new CultureInfo("PT-pt"));
            decimal QtdMassaSolicitada = TestDataGeneratedSolicited.Count();
            int CtsMassaSolicitadas = TestDataGeneratedSolicited.Where(x => x.CasoTesteRelativo != null).Count();
            int BackupMassaSolicitadas = TestDataGeneratedSolicited.Where(x => x.CasoTesteRelativo == null).Count();

            decimal QtdMassaSucesso = TestDataGeneratedSuccessfully.Count();
            int? CtsMassaSucesso = TestDataGeneratedSuccessfully.Where(x => x.CasoTesteRelativo != null).Count();
            int? BackupMassaSucesso = TestDataGeneratedSuccessfully.Where(x => x.CasoTesteRelativo == null).Count();

            decimal QtdMassaErro = TestDataGeneratedFailed.Count();
            int? CtsMassaErro = TestDataGeneratedFailed.Where(x => x.CasoTesteRelativo != null).Count();
            int? BackupMassaErro = TestDataGeneratedFailed.Where(x => x.CasoTesteRelativo == null).Count();

            decimal QtdMassaNoRun = TestDataNoRun.Count();
            int? CtsMassaNoRun = TestDataNoRun.Where(x => x.CasoTesteRelativo != null).Count();
            int? BackupMassaNoRun = TestDataNoRun.Where(x => x.CasoTesteRelativo == null).Count();

            //Montando a mensagem
            //string textoPlantao = "{0}| Backlog Target        {1} = {2} / {3}";
            //textoPlantao = string.Format(textoPlantao, !true ? "\U00002705" : "\U0001F534", 5, 5, 5);

            string message = "<b>Status de execução</b>" + Environment.NewLine;
            message += "<b>" + Demanda + "</b>" + Environment.NewLine;
            message += "Script: " + Script + Environment.NewLine;
            message += "Data: " + Data + Environment.NewLine;
            message += Environment.NewLine;
            message += "Qtd massa solicitadas: " + QtdMassaSolicitada + Environment.NewLine;
            message += "Cts: " + CtsMassaSolicitadas + Environment.NewLine;
            message += "Backup: " + BackupMassaSucesso + Environment.NewLine;
            message += Environment.NewLine;
            message += "Geradas com sucesso: " + QtdMassaSucesso + " (" + Decimal.Round((QtdMassaSucesso / QtdMassaSolicitada * 100), 2) + "%)" + Environment.NewLine;
            message += "Cts: " + CtsMassaSucesso + Environment.NewLine;
            message += "Backup: " + BackupMassaSucesso + Environment.NewLine;
            message += Environment.NewLine;
            message += "Com falhas: " + QtdMassaErro + " (" + Decimal.Round((QtdMassaErro / QtdMassaSolicitada * 100), 2) + "%)" + Environment.NewLine;
            message += "Cts: " + CtsMassaErro + Environment.NewLine;
            message += "Backup: " + BackupMassaErro + Environment.NewLine;
            message += Environment.NewLine;
            message += "Não Executados: " + QtdMassaNoRun + " (" + Decimal.Round((QtdMassaNoRun / QtdMassaSolicitada * 100), 2) + "%)" + Environment.NewLine;
            message += "Cts: " + CtsMassaNoRun + Environment.NewLine;
            message += "Backup: " + BackupMassaNoRun + Environment.NewLine;


            return message;
        }

        //Retorna o IP Local da máquina
        public static string GetIpLocal()
        {

            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

            string ip_local = "Não foi possível identificar o ip da máquina";

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ip_local = ip.ToString();
                }
            }

            return ip_local;
        }

    }

    internal class DadosExecucao
    {
        public string DatapoolName { get; internal set; }
        public string DemandaName { get; internal set; }
        public int IdExecucao { get; set; }
        public int? IdTestData { get; set; }
    }
}

