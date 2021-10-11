using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace VNF.Integration.DataLayer.Model
{
    [Table("TbLOGApplication", Schema = "DBO")]
    public class TbLOGApplication
    {
        [Key]
        [DisplayName("id_log")]
        [Column("ID_LOG")]
        public int id_log { get; set; }

        [DisplayName("log_type")]
        [Column("LOG_TYPE")]
        public string log_type { get; set; }

        [DisplayName("log_user")]
        [Column("LOG_USER")]
        public string log_user { get; set; }

        [DisplayName("log_date")]
        [Column("LOG_DATE")]
        public DateTime? log_date { get; set; }

        [DisplayName("log_title")]
        [Column("LOG_TITLE")]
        public string log_title { get; set; }

        [DisplayName("log_description")]
        [Column("LOG_DESCRIPTION")]
        public string log_description { get; set; }

        [DisplayName("log_link")]
        [Column("LOG_LINK")]
        public string log_link { get; set; }

        [DisplayName("log_nfeid")]
        [Column("LOG_NFEID")]
        public string log_nfeid { get; set; }

        [DisplayName("log_icon")]
        [Column("LOG_ICON")]
        public string log_icon { get; set; }
    }
}