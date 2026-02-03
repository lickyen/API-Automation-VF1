using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class DCServices 
    {
        private readonly DCRepository _dCRepository;

        public DCServices(DCRepository dCRepository)
        {
            _dCRepository = dCRepository;
        }


        public string AddCase(AddCaseDto addCaseDto)
        {
            if (addCaseDto == null)
            {
                throw new ArgumentNullException(nameof(addCaseDto));
            }

            //if (string.IsNullOrWhiteSpace(addCaseDto.EmpCode))
            //{
            //    throw new ArgumentException("UserId cannot be null or empty.");
            //}

            return _dCRepository.AddCase(addCaseDto);
        }

        public string AddCaseEmployees(AddEmployeesToCaseDto addEmployeesToCaseDto)
        {
            if (addEmployeesToCaseDto == null)
            {
                throw new ArgumentNullException(nameof(addEmployeesToCaseDto));
            }

            return _dCRepository.AddCaseEmployees(addEmployeesToCaseDto);
        }

        public List<CaseListDto> GetAllCases()
        {
            return _dCRepository.GetAllCases();
        }

        //public List<CaseDto> GetCasesByEmpid(CaseRequest caseRequest )
        //{

        //    return _dCRepository.GetCasesByEmpid(caseRequest);
        //}

        public CaseDto? GetCase(int caseId)
        {
            if ( caseId <= 0)
            {
                throw new ArgumentException("caseId cannot be less than or equal to Zero.");
            }

            return _dCRepository.GetCase(caseId);
        }
    }
}
