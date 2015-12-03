using System.IO;

public class IndentedStream
{
    Stream stream;
    int indentLevel;
    string indentString;


    public IndentedStream(Stream s, string indent)
    {
        stream = s;
        indentLevel = 0;
        indentString = indent;
    }


    private void WriteString(string str)
    {
        byte[] name = System.Text.Encoding.UTF8.GetBytes(str);
        stream.Write(name, 0, name.Length);
    }


    public void WriteLine(string str)
    {
        for (int i = 0; i < indentLevel; i++)
            WriteString(indentString);

        WriteString(str);
        WriteString("\n");
    }


    public void Indent()
    {
        indentLevel++;
    }


    public void Unindent()
    {
        indentLevel--;
    }
}
