using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSMSServiceSDK
{

    //错误的定义
    public enum ErrorDef
    {
        Ok = 0,//没有错误
        TimeoutErr = 1,//超时
        UnconnectedErr = 2,//没有连接
        SerializeErr = 3,//序列化失败
        SendErr = 4,//发送失败
        UnmarshalErr = 5,//解压回传消息错误
        UnknownErr = 6,//未知错误
    }

    //消息id
    public enum MsgID
    {
        ConnectID = 0,

        DisconnectID = 1,

        GetDefenceZoneRequestID = 2,

        GetDefenceZoneReplyID = 3,

        CreateDefenceZoneNotifyID = 4,

        UpdateDefenceZoneNotifyID = 5,

        UpdateDFStateNotifyID = 6,

        UpdateDFDeploymentStateNotifyID = 7,

        DeleteDFNotifyID = 8,

        DeploymentRequestID = 9,

        DeploymentReplyID = 10,

        WithdrawalRequestID = 11,

        WithdrawalReplyID = 12,

        ResetAlarmRequestID = 13,

        ResetAlarmReplyID = 14,

        CloseAlarmSoundRequestID = 15,

        CloseAlarmSoundReplyID = 16,

        GetHistoryRequestID = 17,

        GetHistoryReplyID = 18,

        HeartBeatID = 250
    }

    //布防状态
    public enum DeploymentState
    {
        Deployment = 0,

        Withdrawal = 1
    }

    //防区状态
    public enum DefenceAreaState
    {
        Normal = 0,

        Alarm = 1,

        Fault = 1
    }

    //防区
    public class DefenceZone {
        public int ID;
        public string ZoneName;
        public float Start;
        public float Finish;
        public DeploymentState State;
        public DefenceAreaState AlarmState;
        public string CreateDate;
        public string Detail;
    }

    //警报记录
    public class AlarmLog
    {
        public int ID;
        public string DeviceName;
        public string AlarmTitle;
        public string AlarmContent;
        public string AlarmDate;
        public double Latitude;
        public double Longitude;
        public string RecordUrl;
        public float AlarmLoction;        
    }

    //获取所有防区
    public class GetDefenceZoneRequest
    {
        public string StartTime;
        public string FinishTime;
        public int Limit;
        public int Offset;
    }

    //获取所有防区
    public class GetDefenceZoneReply
    {
        public bool Success;
        public string ErrMsg;
        public int Total;
        public DefenceZone[] Rows;
    }

    //获取历史记录
    public class GetHistoryRequest
    {
        public string StartTime;
        public string FinishTime;
        public int Limit;
        public int Offset;
    }


    //获取历史记录
    public class GetHistoryReply
    {
        public bool Success;
        public string ErrMsg;
        public int Total;
        public AlarmLog[] Rows;
    }

    //创建防区，广播
    public class CreateDefenceZoneNotify
    {
        public DefenceZone Zone;
    }

    //更新防区，广播
    public class UpdateDefenceZoneNotify
    {
        public DefenceZone Zone;
    }

    //更新防区状态，广播
    public class UpdateDFStateNotify
    {
        public DefenceZone Zone;
    }

    //更新布防状态，广播
    public class UpdateDFDeploymentStateNotify
    {
        public DefenceZone Zone;
    }

    //删除防区，广播
    public class DeleteDFNotify
    {
        public int ID;
    }


    //布防
    public class DeploymentRequest
    {
        public int ID;
    }

    //布防回执
    public class DeploymentReply
    {
        public bool Success;
        public string Err;
    }

    //撤防
    public class WithdrawalRequest
    {
        public int ID;
    }

    //撤防回执
    public class WithdrawalReply
    {
        public bool Success;
        public string Err;
    }


    //重置警报
    public class ResetAlarmRequest
    {

    }

    //重置警报回执
    public class ResetAlarmReply
    {
        public bool Success;
        public string Err;
    }


    //警报消音
    public class CloseAlarmSoundRequest
    {

    }

    //警报消音 回执
    public class CloseAlarmSoundReply
    {
        public bool Success;
        public string Err;
    }


    //心跳
    public class HeartBeat
    {
      
    }

}
