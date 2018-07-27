using System.IO;

namespace Edu.Stanford.Nlp.IO
{
    public interface IFileFilter
    {
         bool Accept(FileInfo file);
    }
}