using System;

namespace QueuingSystem.Drmaa{
    public class DrmaaSession: ISession
    {        
        private static DrmaaSession _instance;
        private readonly string _contact; 
        public static DrmaaSession GetInstance()
        {
            if (_instance == null){
                _instance = new DrmaaSession();                
            }

            return _instance;
        }

        private DrmaaSession(string contact=null)
        {
            _contact = contact;
        }

        private bool _inited;
        public string NativeSpecificationTemplate { get; set; } = "{threads}";
        
        public void Init(){
            if (_inited)
            {
                Console.Error.WriteLine("DRMAA session is already initialized");
                return;
            }
            DrmaaWrapper.Init(_contact);
            _inited = true;
        }

        public QueuingSystem.Status JobStatus(string jobId){
            return MapStatus(DrmaaWrapper.JobPs(jobId));
        }

        private static QueuingSystem.Status MapStatus(Status status)
        {
            switch (status)
            {
                case Status.Running:
                    return QueuingSystem.Status.Running;
                case Status.Done:
                    return QueuingSystem.Status.Success;
                case Status.Failed:
                    return QueuingSystem.Status.Failed;
                default:
                    return QueuingSystem.Status.Unknown;
            }
        }
        
        private static Action MapAction(QueuingSystem.Action action)
        {
            switch (action)
            {
                case QueuingSystem.Action.Suspend:
                    return Action.Suspend;
                case QueuingSystem.Action.Resume:
                    return Action.Resume;
                case QueuingSystem.Action.Terminate:
                    return Action.Terminate;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        public void JobControl(string jobId, QueuingSystem.Action action){
            DrmaaWrapper.Control(jobId, MapAction(action));
        }

        public IJobTemplate AllocateJobTemplate(){
            return new DrmaaJobTemplate(DrmaaWrapper.AllocateJobTemplate(), NativeSpecificationTemplate);
        }

        public QueuingSystem.Status WaitForJobBlocking(string jobId)
        {
            return MapStatus(DrmaaWrapper.Wait(jobId));
        }

        public void Exit()
        {
            DrmaaWrapper.Exit(_contact);
            _inited = false;
        }

        public string Submit(IJobTemplate jobTemplate)
        {
            var jobTemplateDrmaa = jobTemplate as DrmaaJobTemplate;
            return jobTemplateDrmaa.Submit();
        }
    }
}