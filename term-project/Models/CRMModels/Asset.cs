﻿using Postgrest.Attributes;
using Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace term_project.Models.CRMModels
{
  [Table("ASSET")]
  public class Asset : BaseModel
  {
    [PrimaryKey("asset_id")]
    public Guid AssetId { get; set; }

    [Column("type")]
    public string Type { get; set; }

    [Column("status")]
    public string Status { get; set; }

    [Column("rent_history_id")]
    public Guid RentHistoryId { get; set; }

    [Column("application_count")]
    public int ApplicationCount { get; set; }
  }
}
