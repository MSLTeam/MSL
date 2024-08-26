using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HandyControl.Tools;

namespace MSL.langs
{
    public class LangProvider : INotifyPropertyChanged
    {
        internal static LangProvider Instance => ResourceHelper.GetResource<LangProvider>("langs");

        private static string CultureInfoStr;

        public static CultureInfo Culture
        {
            get => Lang.Culture;
            set
            {
                if (value == null) return;
                if (Equals(CultureInfoStr, value.EnglishName)) return;
                Lang.Culture = value;
                CultureInfoStr = value.EnglishName;

                Instance.UpdateLangs();
            }
        }

        public static string GetLang(string key) => Lang.ResourceManager.GetString(key, Culture);

        public static void SetLang(DependencyObject dependencyObject, DependencyProperty dependencyProperty, string key) =>
            BindingOperations.SetBinding(dependencyObject, dependencyProperty, new Binding(key)
            {
                Source = Instance,
                Mode = BindingMode.OneWay
            });

		private void UpdateLangs()
        {
                            OnPropertyChanged(nameof(Dialog_Cancel));
                            OnPropertyChanged(nameof(Dialog_Done));
                            OnPropertyChanged(nameof(Dialog_Err));
                            OnPropertyChanged(nameof(Dialog_Tip));
                            OnPropertyChanged(nameof(Dialog_Warning));
                            OnPropertyChanged(nameof(LangComment));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_AutoLaunchFrpc));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_AutoLaunchFrpcErr));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_AutoLaunchServer));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_AutoLaunchServerErr));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_BeatVersion));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_BetaVersion));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_CheckUpdateErr));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_Close));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_Close2));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_ConfigErr));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_ConfigErr2));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_Eula));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_InitErr));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_LatestVersion));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_MemoryErr));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_MSLServerDown));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_ReadEula));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_RefuseUpdate));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_Update));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_UpdateFailed));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_UpdateInfo1));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_UpdateInfo2));
                            OnPropertyChanged(nameof(MainWindow_GrowlMsg_UpdateWarning));
                            OnPropertyChanged(nameof(MainWindow_Menu_About));
                            OnPropertyChanged(nameof(MainWindow_Menu_Frpc));
                            OnPropertyChanged(nameof(MainWindow_Menu_Home));
                            OnPropertyChanged(nameof(MainWindow_Menu_OnlinePlay));
                            OnPropertyChanged(nameof(MainWindow_Menu_ServerList));
                            OnPropertyChanged(nameof(MainWindow_Menu_Setting));
                            OnPropertyChanged(nameof(Pages_About_AboutMSL));
                            OnPropertyChanged(nameof(Pages_About_MainContent));
                            OnPropertyChanged(nameof(Pages_About_OpenSource));
                            OnPropertyChanged(nameof(Pages_About_OpenWebsite));
                            OnPropertyChanged(nameof(Pages_About_Package));
                            OnPropertyChanged(nameof(Pages_About_Sponsor));
                            OnPropertyChanged(nameof(Pages_About_SponsorText));
                            OnPropertyChanged(nameof(Pages_About_Website));
                            OnPropertyChanged(nameof(Pages_Frpc_Close));
                            OnPropertyChanged(nameof(Pages_Frpc_Copy));
                            OnPropertyChanged(nameof(Pages_Frpc_IP));
                            OnPropertyChanged(nameof(Pages_Frpc_IPNull));
                            OnPropertyChanged(nameof(Pages_Frpc_Launch));
                            OnPropertyChanged(nameof(Pages_Frpc_Status));
                            OnPropertyChanged(nameof(Pages_Frpc_Title));
                            OnPropertyChanged(nameof(Pages_Home_CreateServer));
                            OnPropertyChanged(nameof(Pages_Home_LaunchServer));
                            OnPropertyChanged(nameof(Pages_Home_Notice));
                            OnPropertyChanged(nameof(Pages_Home_P2PPlay));
                            OnPropertyChanged(nameof(Pages_Home_Recommendations));
                            OnPropertyChanged(nameof(Pages_Online_Close));
                            OnPropertyChanged(nameof(Pages_Online_CloseSuc));
                            OnPropertyChanged(nameof(Pages_Online_Create_key));
                            OnPropertyChanged(nameof(Pages_Online_Create_Port));
                            OnPropertyChanged(nameof(Pages_Online_Create_QQn));
                            OnPropertyChanged(nameof(Pages_Online_CreateBtn));
                            OnPropertyChanged(nameof(Pages_Online_DlFrpc));
                            OnPropertyChanged(nameof(Pages_Online_Enter_Key));
                            OnPropertyChanged(nameof(Pages_Online_Enter_Port));
                            OnPropertyChanged(nameof(Pages_Online_Enter_QQn));
                            OnPropertyChanged(nameof(Pages_Online_EnterBtn));
                            OnPropertyChanged(nameof(Pages_Online_Err));
                            OnPropertyChanged(nameof(Pages_Online_ErrMsg1));
                            OnPropertyChanged(nameof(Pages_Online_ExitRoom));
                            OnPropertyChanged(nameof(Pages_Online_Header_Enter));
                            OnPropertyChanged(nameof(Pages_Online_HeaderCreate));
                            OnPropertyChanged(nameof(Pages_Online_Log));
                            OnPropertyChanged(nameof(Pages_Online_LoginSuc));
                            OnPropertyChanged(nameof(Pages_Online_ServerStatusChecking));
                            OnPropertyChanged(nameof(Pages_Online_ServerStatusDown));
                            OnPropertyChanged(nameof(Pages_Online_ServerStatusOK));
                            OnPropertyChanged(nameof(Pages_Online_Suc));
                            OnPropertyChanged(nameof(Pages_Online_Tips1));
                            OnPropertyChanged(nameof(Pages_Online_TipsOpenWeb));
                            OnPropertyChanged(nameof(Pages_Online_Title));
                            OnPropertyChanged(nameof(Pages_Online_UdFrpc));
                            OnPropertyChanged(nameof(Pages_OnlinePage_Dialog_Tips));
                            OnPropertyChanged(nameof(Pages_ServerList_Delete));
                            OnPropertyChanged(nameof(Pages_ServerList_Dialog_FirstUse));
                            OnPropertyChanged(nameof(Pages_ServerList_Do));
                            OnPropertyChanged(nameof(Pages_ServerList_LaunchServer));
                            OnPropertyChanged(nameof(Pages_ServerList_ManageModsOrPlugins));
                            OnPropertyChanged(nameof(Pages_ServerList_OpenDir));
                            OnPropertyChanged(nameof(Pages_ServerList_Refresh));
                            OnPropertyChanged(nameof(Pages_ServerList_ServerName));
                            OnPropertyChanged(nameof(Pages_ServerList_Setting));
                            OnPropertyChanged(nameof(Pages_ServerList_Status));
                            OnPropertyChanged(nameof(Pages_ServerList_Title));
                            OnPropertyChanged(nameof(Pages_ServerList_UseCMDLaunch));
                    }

        /// <summary>
        ///   查找类似 取消 的本地化字符串。
        /// </summary>
		public string Dialog_Cancel => Lang.Dialog_Cancel;

        /// <summary>
        ///   查找类似 确定 的本地化字符串。
        /// </summary>
		public string Dialog_Done => Lang.Dialog_Done;

        /// <summary>
        ///   查找类似 错误 的本地化字符串。
        /// </summary>
		public string Dialog_Err => Lang.Dialog_Err;

        /// <summary>
        ///   查找类似 提示 的本地化字符串。
        /// </summary>
		public string Dialog_Tip => Lang.Dialog_Tip;

        /// <summary>
        ///   查找类似 警告 的本地化字符串。
        /// </summary>
		public string Dialog_Warning => Lang.Dialog_Warning;

        /// <summary>
        ///   查找类似 查找类似 {0} 的本地化字符串。 的本地化字符串。
        /// </summary>
		public string LangComment => Lang.LangComment;

        /// <summary>
        ///   查找类似 正在为你自动打开内网映射…… 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_AutoLaunchFrpc => Lang.MainWindow_GrowlMsg_AutoLaunchFrpc;

        /// <summary>
        ///   查找类似 自动启动内网映射失败！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_AutoLaunchFrpcErr => Lang.MainWindow_GrowlMsg_AutoLaunchFrpcErr;

        /// <summary>
        ///   查找类似 正在为你自动打开相应服务器…… 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_AutoLaunchServer => Lang.MainWindow_GrowlMsg_AutoLaunchServer;

        /// <summary>
        ///   查找类似 自动启动服务器失败！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_AutoLaunchServerErr => Lang.MainWindow_GrowlMsg_AutoLaunchServerErr;

        /// <summary>
        ///   查找类似 当前版本高于正式版本，若使用中遇到BUG，请及时反馈！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_BeatVersion => Lang.MainWindow_GrowlMsg_BeatVersion;

        /// <summary>
        ///   查找类似 当前版本高于最新正式版，若遇到Bug，请及时向开发者反馈！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_BetaVersion => Lang.MainWindow_GrowlMsg_BetaVersion;

        /// <summary>
        ///   查找类似 检查更新失败！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_CheckUpdateErr => Lang.MainWindow_GrowlMsg_CheckUpdateErr;

        /// <summary>
        ///   查找类似 您的服务器、内网映射或联机功能正在运行中，关闭软件可能会让这些进程在后台一直运行并占用资源！确定要继续关闭吗？ 注：如果想隐藏主窗口的话，请前往设置打开托盘图标 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_Close => Lang.MainWindow_GrowlMsg_Close;

        /// <summary>
        ///   查找类似 您的服务器、内网映射或联机功能正在运行中，关闭软件可能会让这些进程在后台一直运行并占用资源！确定要继续关闭吗？ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_Close2 => Lang.MainWindow_GrowlMsg_Close2;

        /// <summary>
        ///   查找类似 MSL在加载配置文件时出现错误，此报错可能不影响软件运行，但还是建议您将其反馈给作者！ 错误代码： 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_ConfigErr => Lang.MainWindow_GrowlMsg_ConfigErr;

        /// <summary>
        ///   查找类似 MSL在加载配置文件时出现错误，将进行重试，若点击确定后软件突然闪退，请尝试使用管理员身份运行或将此问题报告给作者！ 错误代码： 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_ConfigErr2 => Lang.MainWindow_GrowlMsg_ConfigErr2;

        /// <summary>
        ///   查找类似 使用本软件，即代表您已阅读并接受本软件的使用协议：https://www.mslmc.cn/eula.html 如果您不接受，请立即退出本软件并删除相关文件！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_Eula => Lang.MainWindow_GrowlMsg_Eula;

        /// <summary>
        ///   查找类似 MSL在初始化加载过程中出现问题，请尝试用管理员身份运行MSL…… 错误代码： 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_InitErr => Lang.MainWindow_GrowlMsg_InitErr;

        /// <summary>
        ///   查找类似 软件已是最新版本！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_LatestVersion => Lang.MainWindow_GrowlMsg_LatestVersion;

        /// <summary>
        ///   查找类似 获取系统内存失败！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_MemoryErr => Lang.MainWindow_GrowlMsg_MemoryErr;

        /// <summary>
        ///   查找类似 软件主服务器连接超时，已切换至备用服务器！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_MSLServerDown => Lang.MainWindow_GrowlMsg_MSLServerDown;

        /// <summary>
        ///   查找类似 阅读协议 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_ReadEula => Lang.MainWindow_GrowlMsg_ReadEula;

        /// <summary>
        ///   查找类似 您拒绝了更新到新版本，若在此版本中遇到bug，请勿报告给作者！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_RefuseUpdate => Lang.MainWindow_GrowlMsg_RefuseUpdate;

        /// <summary>
        ///   查找类似 更新 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_Update => Lang.MainWindow_GrowlMsg_Update;

        /// <summary>
        ///   查找类似 更新失败！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_UpdateFailed => Lang.MainWindow_GrowlMsg_UpdateFailed;

        /// <summary>
        ///   查找类似 发现新版本，版本号为： 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_UpdateInfo1 => Lang.MainWindow_GrowlMsg_UpdateInfo1;

        /// <summary>
        ///   查找类似 ，是否进行更新？ 更新日志：  的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_UpdateInfo2 => Lang.MainWindow_GrowlMsg_UpdateInfo2;

        /// <summary>
        ///   查找类似 您的服务器/内网映射/点对点联机正在运行中，若此时更新，会造成后台残留，请将前者关闭后再进行更新！ 的本地化字符串。
        /// </summary>
		public string MainWindow_GrowlMsg_UpdateWarning => Lang.MainWindow_GrowlMsg_UpdateWarning;

        /// <summary>
        ///   查找类似 关于 的本地化字符串。
        /// </summary>
		public string MainWindow_Menu_About => Lang.MainWindow_Menu_About;

        /// <summary>
        ///   查找类似 映射 的本地化字符串。
        /// </summary>
		public string MainWindow_Menu_Frpc => Lang.MainWindow_Menu_Frpc;

        /// <summary>
        ///   查找类似 主页 的本地化字符串。
        /// </summary>
		public string MainWindow_Menu_Home => Lang.MainWindow_Menu_Home;

        /// <summary>
        ///   查找类似 联机 的本地化字符串。
        /// </summary>
		public string MainWindow_Menu_OnlinePlay => Lang.MainWindow_Menu_OnlinePlay;

        /// <summary>
        ///   查找类似 服务器 的本地化字符串。
        /// </summary>
		public string MainWindow_Menu_ServerList => Lang.MainWindow_Menu_ServerList;

        /// <summary>
        ///   查找类似 设置 的本地化字符串。
        /// </summary>
		public string MainWindow_Menu_Setting => Lang.MainWindow_Menu_Setting;

        /// <summary>
        ///   查找类似 关于软件 的本地化字符串。
        /// </summary>
		public string Pages_About_AboutMSL => Lang.Pages_About_AboutMSL;

        /// <summary>
        ///   查找类似 Minecraft Server Launcher(MSL) Copyright © 2021-2024 By MSLTeam 本软件与Microsoft、Mojang Studio(Mojang AB)无任何隶属关系 本软件为开源软件，用户使用本软件从事的任何行为均与开发者无关 本软件接入的第三方服务，其相关权利归第三方所有  开服器的正常运行离不开下面的下载源/站点，特此感谢： CurseForge —— 模组、模组整合包下载源API BMCLAPI —— Forge服务端下载源 以及其他服务端官方和第三方下载源……  同时感谢所有使用MSL的用户，以及给MSL提出过改进建议和Bug的用户 感谢各位的支持与信任！ 的本地化字符串。
        /// </summary>
		public string Pages_About_MainContent => Lang.Pages_About_MainContent;

        /// <summary>
        ///   查找类似 开源（Github） 的本地化字符串。
        /// </summary>
		public string Pages_About_OpenSource => Lang.Pages_About_OpenSource;

        /// <summary>
        ///   查找类似 打开网站 的本地化字符串。
        /// </summary>
		public string Pages_About_OpenWebsite => Lang.Pages_About_OpenWebsite;

        /// <summary>
        ///   查找类似 依赖/包 的本地化字符串。
        /// </summary>
		public string Pages_About_Package => Lang.Pages_About_Package;

        /// <summary>
        ///   查找类似 赞助 的本地化字符串。
        /// </summary>
		public string Pages_About_Sponsor => Lang.Pages_About_Sponsor;

        /// <summary>
        ///   查找类似 软件制作不易，您的支持就是我们的动力！ 的本地化字符串。
        /// </summary>
		public string Pages_About_SponsorText => Lang.Pages_About_SponsorText;

        /// <summary>
        ///   查找类似 官方网站/文档：https://www.mslmc.cn/ 的本地化字符串。
        /// </summary>
		public string Pages_About_Website => Lang.Pages_About_Website;

        /// <summary>
        ///   查找类似 关闭内网映射 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_Close => Lang.Pages_Frpc_Close;

        /// <summary>
        ///   查找类似 复制 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_Copy => Lang.Pages_Frpc_Copy;

        /// <summary>
        ///   查找类似 使用此IP来连接： 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_IP => Lang.Pages_Frpc_IP;

        /// <summary>
        ///   查找类似 无 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_IPNull => Lang.Pages_Frpc_IPNull;

        /// <summary>
        ///   查找类似 启动内网映射 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_Launch => Lang.Pages_Frpc_Launch;

        /// <summary>
        ///   查找类似 无 状态 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_Status => Lang.Pages_Frpc_Status;

        /// <summary>
        ///   查找类似 内网映射列表（双击进入详情页） 的本地化字符串。
        /// </summary>
		public string Pages_Frpc_Title => Lang.Pages_Frpc_Title;

        /// <summary>
        ///   查找类似 创建一个新的服务器 的本地化字符串。
        /// </summary>
		public string Pages_Home_CreateServer => Lang.Pages_Home_CreateServer;

        /// <summary>
        ///   查找类似 开启服务器 的本地化字符串。
        /// </summary>
		public string Pages_Home_LaunchServer => Lang.Pages_Home_LaunchServer;

        /// <summary>
        ///   查找类似 公告 的本地化字符串。
        /// </summary>
		public string Pages_Home_Notice => Lang.Pages_Home_Notice;

        /// <summary>
        ///   查找类似 点对点联机 的本地化字符串。
        /// </summary>
		public string Pages_Home_P2PPlay => Lang.Pages_Home_P2PPlay;

        /// <summary>
        ///   查找类似 推荐 的本地化字符串。
        /// </summary>
		public string Pages_Home_Recommendations => Lang.Pages_Home_Recommendations;

        /// <summary>
        ///   查找类似 关闭房间 的本地化字符串。
        /// </summary>
		public string Pages_Online_Close => Lang.Pages_Online_Close;

        /// <summary>
        ///   查找类似 关闭成功！ 的本地化字符串。
        /// </summary>
		public string Pages_Online_CloseSuc => Lang.Pages_Online_CloseSuc;

        /// <summary>
        ///   查找类似 房间密钥： 的本地化字符串。
        /// </summary>
		public string Pages_Online_Create_key => Lang.Pages_Online_Create_key;

        /// <summary>
        ///   查找类似 游戏端口： 的本地化字符串。
        /// </summary>
		public string Pages_Online_Create_Port => Lang.Pages_Online_Create_Port;

        /// <summary>
        ///   查找类似 QQ号： 的本地化字符串。
        /// </summary>
		public string Pages_Online_Create_QQn => Lang.Pages_Online_Create_QQn;

        /// <summary>
        ///   查找类似 点击创建房间 的本地化字符串。
        /// </summary>
		public string Pages_Online_CreateBtn => Lang.Pages_Online_CreateBtn;

        /// <summary>
        ///   查找类似 下载内网映射中··· 的本地化字符串。
        /// </summary>
		public string Pages_Online_DlFrpc => Lang.Pages_Online_DlFrpc;

        /// <summary>
        ///   查找类似 房间密钥： 的本地化字符串。
        /// </summary>
		public string Pages_Online_Enter_Key => Lang.Pages_Online_Enter_Key;

        /// <summary>
        ///   查找类似 绑定端口（默认25565，非必要别改）： 的本地化字符串。
        /// </summary>
		public string Pages_Online_Enter_Port => Lang.Pages_Online_Enter_Port;

        /// <summary>
        ///   查找类似 房主QQ号： 的本地化字符串。
        /// </summary>
		public string Pages_Online_Enter_QQn => Lang.Pages_Online_Enter_QQn;

        /// <summary>
        ///   查找类似 点击加入房间 的本地化字符串。
        /// </summary>
		public string Pages_Online_EnterBtn => Lang.Pages_Online_EnterBtn;

        /// <summary>
        ///   查找类似 桥接失败！ 的本地化字符串。
        /// </summary>
		public string Pages_Online_Err => Lang.Pages_Online_Err;

        /// <summary>
        ///   查找类似 出现错误，请检查是否有杀毒软件误杀并重试: 的本地化字符串。
        /// </summary>
		public string Pages_Online_ErrMsg1 => Lang.Pages_Online_ErrMsg1;

        /// <summary>
        ///   查找类似 退出房间 的本地化字符串。
        /// </summary>
		public string Pages_Online_ExitRoom => Lang.Pages_Online_ExitRoom;

        /// <summary>
        ///   查找类似 进入房间——成员 的本地化字符串。
        /// </summary>
		public string Pages_Online_Header_Enter => Lang.Pages_Online_Header_Enter;

        /// <summary>
        ///   查找类似 创建房间——房主 的本地化字符串。
        /// </summary>
		public string Pages_Online_HeaderCreate => Lang.Pages_Online_HeaderCreate;

        /// <summary>
        ///   查找类似 日志 的本地化字符串。
        /// </summary>
		public string Pages_Online_Log => Lang.Pages_Online_Log;

        /// <summary>
        ///   查找类似 登录服务器成功！ 的本地化字符串。
        /// </summary>
		public string Pages_Online_LoginSuc => Lang.Pages_Online_LoginSuc;

        /// <summary>
        ///   查找类似 服务器状态：检测中 的本地化字符串。
        /// </summary>
		public string Pages_Online_ServerStatusChecking => Lang.Pages_Online_ServerStatusChecking;

        /// <summary>
        ///   查找类似 服务器状态：检测超时，服务器可能下线 的本地化字符串。
        /// </summary>
		public string Pages_Online_ServerStatusDown => Lang.Pages_Online_ServerStatusDown;

        /// <summary>
        ///   查找类似 服务器状态：可用 的本地化字符串。
        /// </summary>
		public string Pages_Online_ServerStatusOK => Lang.Pages_Online_ServerStatusOK;

        /// <summary>
        ///   查找类似 桥接成功！ 的本地化字符串。
        /// </summary>
		public string Pages_Online_Suc => Lang.Pages_Online_Suc;

        /// <summary>
        ///   查找类似 联机教程 的本地化字符串。
        /// </summary>
		public string Pages_Online_Tips1 => Lang.Pages_Online_Tips1;

        /// <summary>
        ///   查找类似 点击打开网站 的本地化字符串。
        /// </summary>
		public string Pages_Online_TipsOpenWeb => Lang.Pages_Online_TipsOpenWeb;

        /// <summary>
        ///   查找类似 点对点联机 的本地化字符串。
        /// </summary>
		public string Pages_Online_Title => Lang.Pages_Online_Title;

        /// <summary>
        ///   查找类似 更新内网映射中··· 的本地化字符串。
        /// </summary>
		public string Pages_Online_UdFrpc => Lang.Pages_Online_UdFrpc;

        /// <summary>
        ///   查找类似 注意：此功能目前不稳定，无法穿透所有类型的NAT，若联机失败，请尝试开服务器并使用内网映射联机！ 该功能可能需要正版账户，若无法联机，请从网络上寻找解决方法或尝试开服务器并使用内网映射联机！ 的本地化字符串。
        /// </summary>
		public string Pages_OnlinePage_Dialog_Tips => Lang.Pages_OnlinePage_Dialog_Tips;

        /// <summary>
        ///   查找类似 删除所选服务器 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Delete => Lang.Pages_ServerList_Delete;

        /// <summary>
        ///   查找类似 开服器检测到配置文件出现了错误，是第一次使用吗？ 是否创建一个新的服务器？ 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Dialog_FirstUse => Lang.Pages_ServerList_Dialog_FirstUse;

        /// <summary>
        ///   查找类似 操作 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Do => Lang.Pages_ServerList_Do;

        /// <summary>
        ///   查找类似 开启服务器 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_LaunchServer => Lang.Pages_ServerList_LaunchServer;

        /// <summary>
        ///   查找类似 管理服务器模组/插件 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_ManageModsOrPlugins => Lang.Pages_ServerList_ManageModsOrPlugins;

        /// <summary>
        ///   查找类似 打开服务器文件夹 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_OpenDir => Lang.Pages_ServerList_OpenDir;

        /// <summary>
        ///   查找类似 刷新 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Refresh => Lang.Pages_ServerList_Refresh;

        /// <summary>
        ///   查找类似 服务器名称 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_ServerName => Lang.Pages_ServerList_ServerName;

        /// <summary>
        ///   查找类似 更改服务器设置 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Setting => Lang.Pages_ServerList_Setting;

        /// <summary>
        ///   查找类似 服务器状态 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Status => Lang.Pages_ServerList_Status;

        /// <summary>
        ///   查找类似 服务器列表（双击可快捷启动服务器） 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_Title => Lang.Pages_ServerList_Title;

        /// <summary>
        ///   查找类似 使用命令行开服 的本地化字符串。
        /// </summary>
		public string Pages_ServerList_UseCMDLaunch => Lang.Pages_ServerList_UseCMDLaunch;


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LangKeys
    {
        /// <summary>
        ///   查找类似 取消 的本地化字符串。
        /// </summary>
		public static string Dialog_Cancel = nameof(Dialog_Cancel);

        /// <summary>
        ///   查找类似 确定 的本地化字符串。
        /// </summary>
		public static string Dialog_Done = nameof(Dialog_Done);

        /// <summary>
        ///   查找类似 错误 的本地化字符串。
        /// </summary>
		public static string Dialog_Err = nameof(Dialog_Err);

        /// <summary>
        ///   查找类似 提示 的本地化字符串。
        /// </summary>
		public static string Dialog_Tip = nameof(Dialog_Tip);

        /// <summary>
        ///   查找类似 警告 的本地化字符串。
        /// </summary>
		public static string Dialog_Warning = nameof(Dialog_Warning);

        /// <summary>
        ///   查找类似 查找类似 {0} 的本地化字符串。 的本地化字符串。
        /// </summary>
		public static string LangComment = nameof(LangComment);

        /// <summary>
        ///   查找类似 正在为你自动打开内网映射…… 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_AutoLaunchFrpc = nameof(MainWindow_GrowlMsg_AutoLaunchFrpc);

        /// <summary>
        ///   查找类似 自动启动内网映射失败！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_AutoLaunchFrpcErr = nameof(MainWindow_GrowlMsg_AutoLaunchFrpcErr);

        /// <summary>
        ///   查找类似 正在为你自动打开相应服务器…… 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_AutoLaunchServer = nameof(MainWindow_GrowlMsg_AutoLaunchServer);

        /// <summary>
        ///   查找类似 自动启动服务器失败！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_AutoLaunchServerErr = nameof(MainWindow_GrowlMsg_AutoLaunchServerErr);

        /// <summary>
        ///   查找类似 当前版本高于正式版本，若使用中遇到BUG，请及时反馈！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_BeatVersion = nameof(MainWindow_GrowlMsg_BeatVersion);

        /// <summary>
        ///   查找类似 当前版本高于最新正式版，若遇到Bug，请及时向开发者反馈！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_BetaVersion = nameof(MainWindow_GrowlMsg_BetaVersion);

        /// <summary>
        ///   查找类似 检查更新失败！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_CheckUpdateErr = nameof(MainWindow_GrowlMsg_CheckUpdateErr);

        /// <summary>
        ///   查找类似 您的服务器、内网映射或联机功能正在运行中，关闭软件可能会让这些进程在后台一直运行并占用资源！确定要继续关闭吗？ 注：如果想隐藏主窗口的话，请前往设置打开托盘图标 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_Close = nameof(MainWindow_GrowlMsg_Close);

        /// <summary>
        ///   查找类似 您的服务器、内网映射或联机功能正在运行中，关闭软件可能会让这些进程在后台一直运行并占用资源！确定要继续关闭吗？ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_Close2 = nameof(MainWindow_GrowlMsg_Close2);

        /// <summary>
        ///   查找类似 MSL在加载配置文件时出现错误，此报错可能不影响软件运行，但还是建议您将其反馈给作者！ 错误代码： 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_ConfigErr = nameof(MainWindow_GrowlMsg_ConfigErr);

        /// <summary>
        ///   查找类似 MSL在加载配置文件时出现错误，将进行重试，若点击确定后软件突然闪退，请尝试使用管理员身份运行或将此问题报告给作者！ 错误代码： 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_ConfigErr2 = nameof(MainWindow_GrowlMsg_ConfigErr2);

        /// <summary>
        ///   查找类似 使用本软件，即代表您已阅读并接受本软件的使用协议：https://www.mslmc.cn/eula.html 如果您不接受，请立即退出本软件并删除相关文件！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_Eula = nameof(MainWindow_GrowlMsg_Eula);

        /// <summary>
        ///   查找类似 MSL在初始化加载过程中出现问题，请尝试用管理员身份运行MSL…… 错误代码： 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_InitErr = nameof(MainWindow_GrowlMsg_InitErr);

        /// <summary>
        ///   查找类似 软件已是最新版本！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_LatestVersion = nameof(MainWindow_GrowlMsg_LatestVersion);

        /// <summary>
        ///   查找类似 获取系统内存失败！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_MemoryErr = nameof(MainWindow_GrowlMsg_MemoryErr);

        /// <summary>
        ///   查找类似 软件主服务器连接超时，已切换至备用服务器！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_MSLServerDown = nameof(MainWindow_GrowlMsg_MSLServerDown);

        /// <summary>
        ///   查找类似 阅读协议 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_ReadEula = nameof(MainWindow_GrowlMsg_ReadEula);

        /// <summary>
        ///   查找类似 您拒绝了更新到新版本，若在此版本中遇到bug，请勿报告给作者！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_RefuseUpdate = nameof(MainWindow_GrowlMsg_RefuseUpdate);

        /// <summary>
        ///   查找类似 更新 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_Update = nameof(MainWindow_GrowlMsg_Update);

        /// <summary>
        ///   查找类似 更新失败！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_UpdateFailed = nameof(MainWindow_GrowlMsg_UpdateFailed);

        /// <summary>
        ///   查找类似 发现新版本，版本号为： 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_UpdateInfo1 = nameof(MainWindow_GrowlMsg_UpdateInfo1);

        /// <summary>
        ///   查找类似 ，是否进行更新？ 更新日志：  的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_UpdateInfo2 = nameof(MainWindow_GrowlMsg_UpdateInfo2);

        /// <summary>
        ///   查找类似 您的服务器/内网映射/点对点联机正在运行中，若此时更新，会造成后台残留，请将前者关闭后再进行更新！ 的本地化字符串。
        /// </summary>
		public static string MainWindow_GrowlMsg_UpdateWarning = nameof(MainWindow_GrowlMsg_UpdateWarning);

        /// <summary>
        ///   查找类似 关于 的本地化字符串。
        /// </summary>
		public static string MainWindow_Menu_About = nameof(MainWindow_Menu_About);

        /// <summary>
        ///   查找类似 映射 的本地化字符串。
        /// </summary>
		public static string MainWindow_Menu_Frpc = nameof(MainWindow_Menu_Frpc);

        /// <summary>
        ///   查找类似 主页 的本地化字符串。
        /// </summary>
		public static string MainWindow_Menu_Home = nameof(MainWindow_Menu_Home);

        /// <summary>
        ///   查找类似 联机 的本地化字符串。
        /// </summary>
		public static string MainWindow_Menu_OnlinePlay = nameof(MainWindow_Menu_OnlinePlay);

        /// <summary>
        ///   查找类似 服务器 的本地化字符串。
        /// </summary>
		public static string MainWindow_Menu_ServerList = nameof(MainWindow_Menu_ServerList);

        /// <summary>
        ///   查找类似 设置 的本地化字符串。
        /// </summary>
		public static string MainWindow_Menu_Setting = nameof(MainWindow_Menu_Setting);

        /// <summary>
        ///   查找类似 关于软件 的本地化字符串。
        /// </summary>
		public static string Pages_About_AboutMSL = nameof(Pages_About_AboutMSL);

        /// <summary>
        ///   查找类似 Minecraft Server Launcher(MSL) Copyright © 2021-2024 By MSLTeam 本软件与Microsoft、Mojang Studio(Mojang AB)无任何隶属关系 本软件为开源软件，用户使用本软件从事的任何行为均与开发者无关 本软件接入的第三方服务，其相关权利归第三方所有  开服器的正常运行离不开下面的下载源/站点，特此感谢： CurseForge —— 模组、模组整合包下载源API BMCLAPI —— Forge服务端下载源 以及其他服务端官方和第三方下载源……  同时感谢所有使用MSL的用户，以及给MSL提出过改进建议和Bug的用户 感谢各位的支持与信任！ 的本地化字符串。
        /// </summary>
		public static string Pages_About_MainContent = nameof(Pages_About_MainContent);

        /// <summary>
        ///   查找类似 开源（Github） 的本地化字符串。
        /// </summary>
		public static string Pages_About_OpenSource = nameof(Pages_About_OpenSource);

        /// <summary>
        ///   查找类似 打开网站 的本地化字符串。
        /// </summary>
		public static string Pages_About_OpenWebsite = nameof(Pages_About_OpenWebsite);

        /// <summary>
        ///   查找类似 依赖/包 的本地化字符串。
        /// </summary>
		public static string Pages_About_Package = nameof(Pages_About_Package);

        /// <summary>
        ///   查找类似 赞助 的本地化字符串。
        /// </summary>
		public static string Pages_About_Sponsor = nameof(Pages_About_Sponsor);

        /// <summary>
        ///   查找类似 软件制作不易，您的支持就是我们的动力！ 的本地化字符串。
        /// </summary>
		public static string Pages_About_SponsorText = nameof(Pages_About_SponsorText);

        /// <summary>
        ///   查找类似 官方网站/文档：https://www.mslmc.cn/ 的本地化字符串。
        /// </summary>
		public static string Pages_About_Website = nameof(Pages_About_Website);

        /// <summary>
        ///   查找类似 关闭内网映射 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_Close = nameof(Pages_Frpc_Close);

        /// <summary>
        ///   查找类似 复制 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_Copy = nameof(Pages_Frpc_Copy);

        /// <summary>
        ///   查找类似 使用此IP来连接： 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_IP = nameof(Pages_Frpc_IP);

        /// <summary>
        ///   查找类似 无 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_IPNull = nameof(Pages_Frpc_IPNull);

        /// <summary>
        ///   查找类似 启动内网映射 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_Launch = nameof(Pages_Frpc_Launch);

        /// <summary>
        ///   查找类似 无 状态 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_Status = nameof(Pages_Frpc_Status);

        /// <summary>
        ///   查找类似 内网映射列表（双击进入详情页） 的本地化字符串。
        /// </summary>
		public static string Pages_Frpc_Title = nameof(Pages_Frpc_Title);

        /// <summary>
        ///   查找类似 创建一个新的服务器 的本地化字符串。
        /// </summary>
		public static string Pages_Home_CreateServer = nameof(Pages_Home_CreateServer);

        /// <summary>
        ///   查找类似 开启服务器 的本地化字符串。
        /// </summary>
		public static string Pages_Home_LaunchServer = nameof(Pages_Home_LaunchServer);

        /// <summary>
        ///   查找类似 公告 的本地化字符串。
        /// </summary>
		public static string Pages_Home_Notice = nameof(Pages_Home_Notice);

        /// <summary>
        ///   查找类似 点对点联机 的本地化字符串。
        /// </summary>
		public static string Pages_Home_P2PPlay = nameof(Pages_Home_P2PPlay);

        /// <summary>
        ///   查找类似 推荐 的本地化字符串。
        /// </summary>
		public static string Pages_Home_Recommendations = nameof(Pages_Home_Recommendations);

        /// <summary>
        ///   查找类似 关闭房间 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Close = nameof(Pages_Online_Close);

        /// <summary>
        ///   查找类似 关闭成功！ 的本地化字符串。
        /// </summary>
		public static string Pages_Online_CloseSuc = nameof(Pages_Online_CloseSuc);

        /// <summary>
        ///   查找类似 房间密钥： 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Create_key = nameof(Pages_Online_Create_key);

        /// <summary>
        ///   查找类似 游戏端口： 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Create_Port = nameof(Pages_Online_Create_Port);

        /// <summary>
        ///   查找类似 QQ号： 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Create_QQn = nameof(Pages_Online_Create_QQn);

        /// <summary>
        ///   查找类似 点击创建房间 的本地化字符串。
        /// </summary>
		public static string Pages_Online_CreateBtn = nameof(Pages_Online_CreateBtn);

        /// <summary>
        ///   查找类似 下载内网映射中··· 的本地化字符串。
        /// </summary>
		public static string Pages_Online_DlFrpc = nameof(Pages_Online_DlFrpc);

        /// <summary>
        ///   查找类似 房间密钥： 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Enter_Key = nameof(Pages_Online_Enter_Key);

        /// <summary>
        ///   查找类似 绑定端口（默认25565，非必要别改）： 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Enter_Port = nameof(Pages_Online_Enter_Port);

        /// <summary>
        ///   查找类似 房主QQ号： 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Enter_QQn = nameof(Pages_Online_Enter_QQn);

        /// <summary>
        ///   查找类似 点击加入房间 的本地化字符串。
        /// </summary>
		public static string Pages_Online_EnterBtn = nameof(Pages_Online_EnterBtn);

        /// <summary>
        ///   查找类似 桥接失败！ 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Err = nameof(Pages_Online_Err);

        /// <summary>
        ///   查找类似 出现错误，请检查是否有杀毒软件误杀并重试: 的本地化字符串。
        /// </summary>
		public static string Pages_Online_ErrMsg1 = nameof(Pages_Online_ErrMsg1);

        /// <summary>
        ///   查找类似 退出房间 的本地化字符串。
        /// </summary>
		public static string Pages_Online_ExitRoom = nameof(Pages_Online_ExitRoom);

        /// <summary>
        ///   查找类似 进入房间——成员 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Header_Enter = nameof(Pages_Online_Header_Enter);

        /// <summary>
        ///   查找类似 创建房间——房主 的本地化字符串。
        /// </summary>
		public static string Pages_Online_HeaderCreate = nameof(Pages_Online_HeaderCreate);

        /// <summary>
        ///   查找类似 日志 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Log = nameof(Pages_Online_Log);

        /// <summary>
        ///   查找类似 登录服务器成功！ 的本地化字符串。
        /// </summary>
		public static string Pages_Online_LoginSuc = nameof(Pages_Online_LoginSuc);

        /// <summary>
        ///   查找类似 服务器状态：检测中 的本地化字符串。
        /// </summary>
		public static string Pages_Online_ServerStatusChecking = nameof(Pages_Online_ServerStatusChecking);

        /// <summary>
        ///   查找类似 服务器状态：检测超时，服务器可能下线 的本地化字符串。
        /// </summary>
		public static string Pages_Online_ServerStatusDown = nameof(Pages_Online_ServerStatusDown);

        /// <summary>
        ///   查找类似 服务器状态：可用 的本地化字符串。
        /// </summary>
		public static string Pages_Online_ServerStatusOK = nameof(Pages_Online_ServerStatusOK);

        /// <summary>
        ///   查找类似 桥接成功！ 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Suc = nameof(Pages_Online_Suc);

        /// <summary>
        ///   查找类似 联机教程 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Tips1 = nameof(Pages_Online_Tips1);

        /// <summary>
        ///   查找类似 点击打开网站 的本地化字符串。
        /// </summary>
		public static string Pages_Online_TipsOpenWeb = nameof(Pages_Online_TipsOpenWeb);

        /// <summary>
        ///   查找类似 点对点联机 的本地化字符串。
        /// </summary>
		public static string Pages_Online_Title = nameof(Pages_Online_Title);

        /// <summary>
        ///   查找类似 更新内网映射中··· 的本地化字符串。
        /// </summary>
		public static string Pages_Online_UdFrpc = nameof(Pages_Online_UdFrpc);

        /// <summary>
        ///   查找类似 注意：此功能目前不稳定，无法穿透所有类型的NAT，若联机失败，请尝试开服务器并使用内网映射联机！ 该功能可能需要正版账户，若无法联机，请从网络上寻找解决方法或尝试开服务器并使用内网映射联机！ 的本地化字符串。
        /// </summary>
		public static string Pages_OnlinePage_Dialog_Tips = nameof(Pages_OnlinePage_Dialog_Tips);

        /// <summary>
        ///   查找类似 删除所选服务器 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Delete = nameof(Pages_ServerList_Delete);

        /// <summary>
        ///   查找类似 开服器检测到配置文件出现了错误，是第一次使用吗？ 是否创建一个新的服务器？ 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Dialog_FirstUse = nameof(Pages_ServerList_Dialog_FirstUse);

        /// <summary>
        ///   查找类似 操作 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Do = nameof(Pages_ServerList_Do);

        /// <summary>
        ///   查找类似 开启服务器 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_LaunchServer = nameof(Pages_ServerList_LaunchServer);

        /// <summary>
        ///   查找类似 管理服务器模组/插件 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_ManageModsOrPlugins = nameof(Pages_ServerList_ManageModsOrPlugins);

        /// <summary>
        ///   查找类似 打开服务器文件夹 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_OpenDir = nameof(Pages_ServerList_OpenDir);

        /// <summary>
        ///   查找类似 刷新 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Refresh = nameof(Pages_ServerList_Refresh);

        /// <summary>
        ///   查找类似 服务器名称 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_ServerName = nameof(Pages_ServerList_ServerName);

        /// <summary>
        ///   查找类似 更改服务器设置 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Setting = nameof(Pages_ServerList_Setting);

        /// <summary>
        ///   查找类似 服务器状态 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Status = nameof(Pages_ServerList_Status);

        /// <summary>
        ///   查找类似 服务器列表（双击可快捷启动服务器） 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_Title = nameof(Pages_ServerList_Title);

        /// <summary>
        ///   查找类似 使用命令行开服 的本地化字符串。
        /// </summary>
		public static string Pages_ServerList_UseCMDLaunch = nameof(Pages_ServerList_UseCMDLaunch);

    }
}