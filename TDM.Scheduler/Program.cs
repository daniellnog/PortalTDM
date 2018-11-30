namespace TDM.TaskScheduler
{
    class Program
    {
        static void Main()
        {

        }

        TaskScheduler oAgendador;
        //Para tratar a definição da tarefa
        ITaskDefinition oDefinicaoTarefa;
        //Para tratar a informação do Trigger
        ITimeTrigger oTrigger;
        //Para tratar a informação da Ação
        IExecAction oAcao;

        //private void btnCriarTarefa_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        oAgendador = new TaskScheduler();
        //        oAgendador.Connect();

        //        //Atribuindo Definição de tarefa
        //        AtribuiDefinicaoTarefa();
        //        //Definindo a informação do gatilho da tarefa
        //        DefineInformacaoGatilho();
        //        //Definindo a informção da ação da tarefa
        //        DefineInformacaoAcao();

        //        //Obtendo a pasta raiz
        //        ITaskFolder root = oAgendador.GetFolder("\\");
        //        //Registrando a tarefa , se a tarefa ja estiver registrada então ela será atualizada
        //        IRegisteredTask regTask = root.RegisterTaskDefinition("_ATMP_Tarefa", oDefinicaoTarefa, (int)_TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null, _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");

        //        //Para executar a tarefa imediatamenteo chamamos o método Run()
        //        //IRunningTask runtask = regTask.Run(null);
        //        //exibe mensagem
        //        MessageBox.Show("Tarefa foi criada com sucesso", "Criar Tarefa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        //exibe erros
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        ////Atribuição da da Definição da tarefa
        //private void AtribuiDefinicaoTarefa()
        //{
        //    try
        //    {
        //        oDefinicaoTarefa = oAgendador.NewTask(0);
        //        //Registra informação para a tarefa
        //        //nome do autor da tarefa
        //        oDefinicaoTarefa.RegistrationInfo.Author = "Gustavo";
        //        //descrição da tarefa
        //        oDefinicaoTarefa.RegistrationInfo.Description = "Executar TestData id X.";
        //        //Registro da data da tarefa
        //        oDefinicaoTarefa.RegistrationInfo.Date = DateTime.Today.ToString("yyyy-MM-ddTHH:mm:ss"); //formatacao

        //        //Definição da tarefa
        //        //Prioridade da Thread
        //        oDefinicaoTarefa.Settings.Priority = 7;
        //        //Habilita a tarefa
        //        oDefinicaoTarefa.Settings.Enabled = true;
        //        //Para ocultar/exibir a tarefa
        //        oDefinicaoTarefa.Settings.Hidden = false;
        //        //Tempo de execução limite para a tarefa
        //        oDefinicaoTarefa.Settings.ExecutionTimeLimit = "PT10M"; //10 minutos
        //        //Define que não precisa de conexão de rede
        //        oDefinicaoTarefa.Settings.RunOnlyIfNetworkAvailable = false;

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        ////Definindo a informação do Gatilho (Trigger)
        //private void DefineInformacaoGatilho()
        //{
        //    try
        //    {
        //        //informação do gatilho baseada no tempo - TASK_TRIGGER_TIME
        //        oTrigger = (ITimeTrigger)oDefinicaoTarefa.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_TIME);
        //        //ID do Trigger
        //        oTrigger.Id = "Trigger_Da_Tarefa";
        //        //hora de inicio
        //        oTrigger.StartBoundary = "2018-10-11T15:00:00"; //yyyy-MM-ddTHH:mm:ss
        //        //hora de encerramento
        //        oTrigger.EndBoundary = "2018-10-11T16:00:00"; //yyyy-MM-ddTHH:mm:ss
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        ////Define a informação da Ação da tarefa
        //private void DefineInformacaoAcao()
        //{
        //    try
        //    {
        //        //Informação da Ação baseada no exe- TASK_ACTION_EXEC
        //        oAcao = (IExecAction)oDefinicaoTarefa.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
        //        //ID da Ação
        //        oAcao.Id = "testeAcao1";
        //        //Define o caminho do arquivo EXE a executar (Vamos abrir o Paint)
        //        oAcao.Path = "mspaint";
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private void btnDeletar_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        //cria instância do agendador
        //        TaskScheduler oAgendador = new TaskScheduler();
        //        oAgendador.Connect();

        //        ITaskFolder containingFolder = oAgendador.GetFolder("\\");
        //        //Deleta a tarefa
        //        containingFolder.DeleteTask("_ATMP_Tarefa", 0);  //da o nome da tarefa que foi criada
        //        MessageBox.Show("Tarefa Deletada...");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "Deletar Tarefa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //}
    }
}
