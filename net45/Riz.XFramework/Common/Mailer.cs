using System;
using System.IO;
using System.Net.Mail;

namespace Riz.XFramework
{
    /// <summary>
    /// Mailer 发送Email的类
    /// </summary>
    public class Mailer
    {
        #region 私有属性

        private const string ADRESSISEMPTY = "email address is empty.";
        private const string ADRESSINVALID = "invalid email address";
        private string _to;               //收件人
        private string _cc;               //抄送
        private string _content;          //邮件内容
        private string _subject;          //主题
        private string _from;             //发件人
        private string _displayName;      //发件人显示的名称
        private string _smtpServer;       //邮件发送服务器地址或dns
        private int _port;                //服务器端口
        private string _password;         //发件箱密码
        private bool _isBodyHtml;         //邮件内容是否以html的形式
        private string _attachments = ""; //文本附件路径
        private MailPriority _priority;   //邮件的级别
        private static readonly object _syncObj = new object();

        #endregion

        #region 公开属性

        /// <summary>
        /// 收件人地址,可以是以;或,分隔的mail地址
        /// 标准的方式是以,(逗号)分隔
        /// </summary>
        public string To
        {
            get { return _to; }
            set { _to = CheckMailAddress(value, true); }
        }

        /// <summary>
        /// 收件人地址,可以是以;或,分隔的mail地址
        /// 标准的方式是以,(逗号)分隔
        /// </summary>
        public string CC
        {
            get { return _cc; }
            set { _cc = CheckMailAddress(value, false); }
        }

        /// <summary>
        /// 邮件主题
        /// </summary>
        public string Subject
        {
            get { return _subject; }
            set { _subject = value; }
        }

        /// <summary>
        /// 邮件内容
        /// </summary>
        public string Content
        {
            get { return _content; }
            set { _content = value; }
        }

        /// <summary>
        /// 发件人地址
        /// </summary>
        public string From
        {
            get { return _from; }
            set { _from = value; }
        }

        /// <summary>
        /// 发件人显示的名称
        /// </summary>
        public string DisplayName
        {
            set
            {
                _displayName = value;
            }
            get
            {
                return _displayName;
            }
        }

        /// <summary>
        /// 邮件发送服务器地址或dns
        /// </summary>
        public string SmtpServer
        {
            get { return _smtpServer; }
            set { _smtpServer = value; }
        }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// 发件箱密码,可以为空
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// 邮件内容是否以html的形式, 默认为false
        /// </summary>
        public bool IsBodyHtml
        {
            get { return _isBodyHtml; }
            set { _isBodyHtml = value; }
        }

        /// <summary>
        /// 文本附件路径,多个附件路径用分号分隔开
        /// </summary>
        public string Attachments
        {
            get { return _attachments; }
            set { _attachments = value; }
        }

        /// <summary>
        /// 邮件的级别,默认为正常Normal
        /// </summary>
        public System.Net.Mail.MailPriority Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// Mailer 构造函数
        /// </summary>
        public Mailer()
        {
            _isBodyHtml = false;
            _priority = System.Net.Mail.MailPriority.Normal;
            _subject = "";
            _content = "";
            _port = 25;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 发smtp邮件
        /// </summary>
        public void SendMail()
        {
            MailMessage mail = null;
            try
            {
                mail = new System.Net.Mail.MailMessage(this.From, this.To);
                mail.Subject = this._subject;
                mail.IsBodyHtml = this._isBodyHtml;
                mail.Body = this._content;
                mail.BodyEncoding = System.Text.Encoding.UTF8;//正文编码
                mail.Priority = this._priority;
                if (!string.IsNullOrEmpty(this.DisplayName)) mail.From = new MailAddress(this.From, DisplayName);//账号名称

                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(this.SmtpServer);
                if (this.Port != 0)
                {
                    smtp.Port = this.Port;
                    smtp.EnableSsl = true;
                }
                if (!string.IsNullOrEmpty(this.CC)) mail.CC.Add(this.CC);//抄送人
                this.Attachment(mail);


                smtp.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                if (!string.IsNullOrEmpty(this.Password))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential(this._from, this._password);
                }
                smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;


                //发送邮件
                smtp.Send(mail);
            }
            finally
            {
                if (mail != null) mail.Dispose();
            }
        }

        #endregion

        #region 辅助函数

        //检查mail地址，对于抄送，则允许地址为空
        private string CheckMailAddress(string address, bool checkEmpty)
        {
            if (string.IsNullOrEmpty(address))
            {
                if (checkEmpty)
                {
                    //邮件地址不能为 null.
                    throw new XFrameworkException(ADRESSISEMPTY);
                }
                else
                {
                    return "";
                }
            }

            address = address.Replace(';', ',');
            if (address.Substring(address.Length - 1, 1) == ",")
                address = address.Substring(0, address.Length - 1);
            string[] tos = address.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tos.Length; i++)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(tos[i], @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"))
                {
                    //无效的邮件地址.
                    throw new XFrameworkException(ADRESSINVALID + address + "'");
                }
            }

            return address;
        }

        //添加附件
        private void Attachment(MailMessage mail)
        {
            if (string.IsNullOrEmpty(Attachments)) return;
            string[] attachments = Attachments.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < attachments.Length; i++)
            {
                if (File.Exists(attachments[i])) mail.Attachments.Add(new Attachment(attachments[i]));
            }
        }

        #endregion

    }
}
