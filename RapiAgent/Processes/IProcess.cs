using System.IO;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Processes
{
    interface IProcess
    {
        int Id { get; }
        Stream StdoutOrMix { get; }
        Stream? Stderr { get; }
        Stream StdIn { get; }
        void Kill();
        Task<int> ExitCode { get; }
    }

    interface IProcessFactory
    {
        IProcess Create(ProcessCreationOptions options);
    }
}