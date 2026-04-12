using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Api.Controllers.AccessControl.Dtos;

public sealed class TableAccessCheckRequest
{
   
    [Required]
    public string TableName { get; set; } = string.Empty;

    
    public string? SchemaName { get; set; }
}
