namespace MundoAwoo.Domain.ValueObjects;

// Un record compara por valor: dos colores #FFFFFF se consideran iguales.
// No tiene Id y no se modifica: para cambiar el color creamos otro objeto.
public sealed record ColorHex
{
    public string Valor { get; }

    public ColorHex(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("El color es obligatorio.", nameof(valor));

        string normalizado = valor.Trim().ToUpperInvariant();

        if (normalizado.Length != 7 || normalizado[0] != '#')
            throw new ArgumentException("Usa el formato #RRGGBB, por ejemplo #FFFFFF.", nameof(valor));

        // Cada carácter después de # debe ser un dígito hexadecimal.
        for (int i = 1; i < normalizado.Length; i++)
        {
            char caracter = normalizado[i];
            bool esNumero = caracter >= '0' && caracter <= '9';
            bool esLetraHex = caracter >= 'A' && caracter <= 'F';

            if (!esNumero && !esLetraHex)
                throw new ArgumentException("El color contiene caracteres inválidos.", nameof(valor));
        }

        Valor = normalizado;
    }

    public override string ToString() => Valor;
}
