using LaefazWeb.Enumerators;
using LaefazWeb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


namespace TDM.Jenkins.PosBuild
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Carregando Query do Tosca");

                Console.WriteLine("1");

                DbEntities db = new DbEntities();

                string ip = GetIpLocal();

                DadosExec CarregaDadosExecucao =
                (from exec in db.Execucao
                 join sca in db.Script_CondicaoScript_Ambiente on exec.IdScript_CondicaoScript_Ambiente equals sca.Id
                 join ambv in db.AmbienteVirtual on sca.IdAmbienteVirtual equals ambv.Id
                 join scs in db.Script_CondicaoScript on sca.IdScript_CondicaoScript equals scs.Id
                 where ambv.IP == ip && exec.IdStatusExecucao == (int)EnumStatusExecucao.AguardandoProcessamento

                 select new DadosExec
                 {
                     QueryTosca = exec.ToscaInput
                    ,
                     ListaExecucaoTosca = scs.ListaExecucaoTosca
                    ,
                     CaminhoArquivoTCS = scs.CaminhoArquivoTCS
                    ,
                     DiretorioRelatorio = scs.DiretorioRelatorio
                    ,
                     NomeQueryTosca = scs.ListaExecucaoTosca  


                 }).FirstOrDefault();

                Console.WriteLine("2");

                List<CountTestData> QueryCountTestData = (
                from exec in db.Execucao
                join sca in db.Script_CondicaoScript_Ambiente on exec.IdScript_CondicaoScript_Ambiente equals sca.Id
                join ambv in db.AmbienteVirtual on sca.IdAmbienteVirtual equals ambv.Id
                join scs in db.Script_CondicaoScript on sca.IdScript_CondicaoScript equals scs.Id
                where ambv.IP == ip && exec.IdStatusExecucao == (int)EnumStatusExecucao.AguardandoProcessamento
                select new CountTestData
                {
                    IdTestData = exec.IdTestData
                    ,
                    IdExecucao = exec.Id

                }).ToList();

                Console.WriteLine("3");

                foreach (var tdids in QueryCountTestData)
                {
                    Console.WriteLine("3.1");
                    TestData td = new TestData();
                    td = db.TestData.FirstOrDefault(x => x.Id == tdids.IdTestData);
                    Console.WriteLine("3.2");
                    td.IdExecucao = tdids.IdExecucao;
                    Console.WriteLine("3.3");
                    db.TestData.Attach(td);
                    Console.WriteLine("3.4");
                    db.Entry(td).State = System.Data.Entity.EntityState.Modified;
                    Console.WriteLine("3.5");
                    db.SaveChanges();

                }

                Console.WriteLine("Criando Query do Tosca");

                if (CarregaDadosExecucao != null)
                {
                    CriaArquivoQUERY(CarregaDadosExecucao.NomeQueryTosca,CarregaDadosExecucao.QueryTosca);
                }
                else
                {
                    Console.WriteLine("Query não encontrada no banco de dados");
                    Console.ReadLine();
                }


                //criação do script tcs que será usado pelo tcshell do tosca
                Console.WriteLine("Criando Arquivo TCS do Tcshell");

                string tcs = ConfigurationSettings.AppSettings["ArquivoTCS"];

                tcs = tcs.Replace("@QuebraLinha ", Environment.NewLine);
                tcs = tcs.Replace("@LimpaLog", "\"Clear Log\"");
                tcs = tcs.Replace("@ListaExecucaoTosca", "\"" + CarregaDadosExecucao.ListaExecucaoTosca + "\"");
                tcs = tcs.Replace("@NomeRelatorio", "\"Print Report ... TDMREPORT\"");
                tcs = tcs.Replace("@DiretorioRelatorio", "\"" + CarregaDadosExecucao.DiretorioRelatorio + "\"");

                CriaArquivoTCS(tcs, CarregaDadosExecucao.CaminhoArquivoTCS);

                Console.WriteLine("tcs = " + tcs);

                List<string> strcmdtext = new List<string>();
                strcmdtext.Add(@"tcshell -workspace");
                //strcmdtext.Add(@" ""C:\Tosca_Projects\Tosca_Workspaces\Workspace_TRG_Fev\portal_remote\portal_remote.tws"" ");
                strcmdtext.Add(@" ""C:\Tosca_Projects\Tosca_Workspaces\TDM\TDM.tws"" ");
                strcmdtext.Add(" -executionmode");
                strcmdtext.Add(@" """ + CarregaDadosExecucao.CaminhoArquivoTCS + "\" ");

                Console.WriteLine("Criando Comando para executar o Tcshell");
                var comando = string.Join(string.Empty, strcmdtext.ToArray());

                Console.WriteLine("Comando = " + comando);
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.Start();

                cmd.StandardInput.WriteLine(@"cd\");
                //cmd.StandardInput.WriteLine(@"cd C:\Tosca_Projects\Tosca_Workspaces\Workspace_TRG_Fev\portal_remote\remote_exec");
                cmd.StandardInput.WriteLine(@"cd C:\Tosca_Projects\Tosca_Workspaces\TDM\remote_exec");
                Console.WriteLine("Executando o Tcshell");
                cmd.StandardInput.WriteLine(comando);
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();

                Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                string output = cmd.StandardOutput.ReadToEnd().ToString();
                Console.WriteLine("output: " + output);
                string outputerro = cmd.StandardError.ReadToEnd().ToString();
                Console.WriteLine("erro: " + outputerro);
                Console.ReadLine();


            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
                Console.ReadLine();
            }



        }



        public static void CriaArquivoTCS(string cmd, string caminho_arquivo_tcs)
        {

            // Passa o texto para uma variável.
            string[] lines = { cmd };

            //Escreve as linhas no arquivo e escolhe aonde salvar.
            System.IO.File.WriteAllLines(@caminho_arquivo_tcs, lines);

        }


        public static void CriaArquivoQUERY(string queryname, string cmd)
        {

            //Passa o texto para uma variável
            string[] lines = { cmd };
            char separador = '/';
            string[] querynamearray = queryname.Split(separador);

            queryname = querynamearray.Last();

            Console.WriteLine("Nome da Query = " + queryname);

            System.IO.File.WriteAllLines(@"C:\Tosca_Projects\Tosca_Templates\queryes\query_" + queryname + ".txt", lines);
        }

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

    internal class CountTestData
    {
        public int IdExecucao { get; internal set; }
        public int? IdTestData { get; internal set; }
    }

    internal class DadosExec
    {
        public string CaminhoArquivoTCS { get; internal set; }
        public object DiretorioRelatorio { get; internal set; }
        public string ListaExecucaoTosca { get; internal set; }
        public string NomeQueryTosca { get; internal set; }
        public string QueryTosca { get; set; }
    }
}



