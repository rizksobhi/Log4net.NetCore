using System;
using System.ComponentModel.DataAnnotations;

namespace Log4net.NetCore.Data.Models
{
    public class Log
    {
        [Key]
        public virtual int LogId { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public DateTime Date { get; set; }
        public string Thread { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
    }
}
