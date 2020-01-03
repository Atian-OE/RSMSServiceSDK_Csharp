using RSMSServiceSDK;
using System;

namespace c_shape
{
    class Program
    {
        static void Connected(string addr) {
            Console.WriteLine("连接成功{0}",addr);
        }
        static void Disconnected(string addr)
        {
            Console.WriteLine("断开连接{0}",addr);
        }
        static void ErrorCallBack(Exception ex)
        {
            Console.WriteLine(ex);
        }
        static void CreateDefenceZoneAction(CreateDefenceZoneNotify notify)
        {
            Console.WriteLine("CreateDefenceZoneNotify:"+notify.Zone.ID);
        }
        static void UpdateDefenceZoneAction(UpdateDefenceZoneNotify notify)
        {
            Console.WriteLine("UpdateDefenceZoneNotify:" + notify.Zone.ID + notify.Zone.AlarmState);
        }
        static void UpdateDFStateAction(UpdateDFStateNotify notify)
        {
            Console.WriteLine("UpdateDFStateNotify:" + notify.Zone.ID);
        }
        static void UpdateDFDeploymentStateAction(UpdateDFDeploymentStateNotify notify)
        {
            Console.WriteLine("UpdateDFDeploymentStateNotify:" + notify.Zone.ID);
        }
        static void DeleteDFAction(DeleteDFNotify notify)
        {
            Console.WriteLine("DeleteDFNotify:" + notify.ID);
        }

        static void Main(string[] args)
        {
            RSMSServiceClient clinet = new RSMSServiceClient("127.0.0.1");
           


            clinet.ConnectedAction += Connected;
            clinet.DisconnectedAction += Disconnected;
            clinet.ErrorAction += ErrorCallBack;
            clinet.CreateDefenceZoneAction += CreateDefenceZoneAction;
            clinet.UpdateDefenceZoneAction += UpdateDefenceZoneAction;
            clinet.UpdateDFStateAction += UpdateDFStateAction;
            clinet.UpdateDFDeploymentStateAction += UpdateDFDeploymentStateAction;
            clinet.DeleteDFAction += DeleteDFAction;
            clinet.Start();


            GetDefenceZoneReply reply;
            ErrorDef error= clinet.GetDefenceZone(0, 10, DateTime.Now.AddDays(-100), DateTime.Now, out reply);
            Console.WriteLine("GetDefenceZone" + error);

            GetHistoryReply reply2;
            error = clinet.GetHistory(0, 10, DateTime.Now.AddDays(-100), DateTime.Now, out reply2);
            Console.WriteLine("GetHistory" + error);

            DeploymentReply reply3;
            error = clinet.Deployment(39, out reply3);
            Console.WriteLine("Deployment" + error);

            WithdrawalReply reply4;
            error = clinet.Withdrawal(39, out reply4);
            Console.WriteLine("Withdrawal" + error);

            ResetAlarmReply reply5;
            error = clinet.ResetAlarm(out reply5);
            Console.WriteLine("ResetAlarm" + error);

            CloseAlarmSoundReply reply6;
            error = clinet.CloseAlarmSound(out reply6);
            Console.WriteLine("CloseAlarmSound" + error);


           
            Console.ReadKey();
            clinet.Close();
        }
    }
}
