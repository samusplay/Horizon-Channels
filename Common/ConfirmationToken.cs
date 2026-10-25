using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Horizon.Channels.Common;

// Genera el token secreto que va en el correo de confirmación del canal.
// Regla: el TOKEN viaja por correo y NUNCA se guarda; en la base solo se guarda su HASH.
public static class ConfirmationToken
{
    // Devuelve los dos valores a la vez: (Token, Hash)
    public static (string Token, string Hash) Generate()
    {
        // 32 bytes aleatorios seguros (256 bits) -> imposibles de adivinar
        // Base64Url los convierte en texto apto para una URL (sin +, / ni =)
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        return (token, Hash(token));
    }

    // Convierte un token en su huella SHA-256 de 64 caracteres hexadecimales.
    // Se usa al crear (para guardar) y al confirmar (para buscar el canal por su hash).
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}