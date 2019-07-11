using RSMSServiceSDK;
using System;

namespace c_shape
{
    class Program
    {
        static void Connected() {
            Console.WriteLine("连接成功");
        }
        static void Disconnected()
        {
            Console.WriteLine("断开连接");
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
            Console.WriteLine("UpdateDefenceZoneNotify:" + notify.Zone.ID);
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
            RSMSServiceDLL.Connected += Connected;
            RSMSServiceDLL.Disconnected += Disconnected;
            RSMSServiceDLL.ErrorAction += ErrorCallBack;
            RSMSServiceDLL.CreateDefenceZoneAction += CreateDefenceZoneAction;
            RSMSServiceDLL.UpdateDefenceZoneAction += UpdateDefenceZoneAction;
            RSMSServiceDLL.UpdateDFStateAction += UpdateDFStateAction;
            RSMSServiceDLL.UpdateDFDeploymentStateAction += UpdateDFDeploymentStateAction;
            RSMSServiceDLL.DeleteDFAction += DeleteDFAction;

            RSMSServiceDLL.InitSDK("127.0.0.1");

            GetDefenceZoneReply reply;
            ErrorDef error= RSMSServiceDLL.GetDefenceZone(0, 10, DateTime.Now.AddDays(-100), DateTime.Now, out reply);
            Console.WriteLine("GetDefenceZone" + error);

            GetHistoryReply reply2;
            error = RSMSServiceDLL.GetHistory(0, 10, DateTime.Now.AddDays(-100), DateTime.Now, out reply2);
            Console.WriteLine("GetHistory" + error);

            DeploymentReply reply3;
            error = RSMSServiceDLL.Deployment(39, out reply3);
            Console.WriteLine("Deployment" + error);

            WithdrawalReply reply4;
            error = RSMSServiceDLL.Withdrawal(39, out reply4);
            Console.WriteLine("Withdrawal" + error);

            ResetAlarmReply reply5;
            error = RSMSServiceDLL.ResetAlarm(out reply5);
            Console.WriteLine("ResetAlarm" + error);

            CloseAlarmSoundReply reply6;
            error = RSMSServiceDLL.CloseAlarmSound(out reply6);
            Console.WriteLine("CloseAlarmSound" + error);


           
            Console.ReadKey();
            RSMSServiceDLL.DestroySDK();
        }
    }
}
