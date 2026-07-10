using System.Text;

namespace RapidFuzz;

public static class Utils
{
    public static string DefaultProcess(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        bool pendingSpace = false;

        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingSpace = false;
            }
            else if (builder.Length > 0)
            {
                pendingSpace = true;
            }
        }

        return builder.ToString();
    }
}
