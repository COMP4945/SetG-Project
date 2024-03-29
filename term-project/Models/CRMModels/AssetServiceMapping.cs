﻿using Postgrest.Attributes;
using Postgrest.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace term_project.Models.CRMModels
{
  [Table("ASSET_SERVICE_MAPPING")]
  public class AssetServiceMapping : BaseModel
  {
    [PrimaryKey("asset_service_id")]
    public Guid AssetServiceId { get; set; }

    [Column("asset_id")]
    public Guid AssetId { get; set; }
  }
}
