namespace GestionAlquileres.Application.Common.DTOs;

/// <summary>Bytes de un PDF generado on-demand (nunca persistido) más el nombre sugerido de archivo.</summary>
public record PdfFileDto(byte[] Content, string FileName);
