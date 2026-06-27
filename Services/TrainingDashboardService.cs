using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;

namespace ProductivityApi.Services
{
    public class TrainingDashboardService
    {
        private readonly TrainingDashboardRepository _repository;

        public TrainingDashboardService(
            TrainingDashboardRepository repository)
        {
            _repository = repository;
        }

        public List<TrainingMasterModel> GetTrainingNames()
        {
            return _repository.GetTrainingNames();
        }

        public List<TrainingLocationModel> GetLocations()
        {
            return _repository.GetLocations();
        }

        public List<TrainingEmployeeModel> GetEmployeeList(string userId)
        {
            return _repository.GetEmployeeList(userId);
        }

        public int AssignTraining(AssignTrainingRequestModel model)
        {
            return _repository.AssignTraining(model);
        }

        public List<object> BulkAssignTraining(
            BulkAssignTrainingModel model)
        {
            List<object> results = new();

            foreach (var empId in model.EmpIDs)
            {
                if (string.IsNullOrWhiteSpace(empId))
                {
                    results.Add(new
                    {
                        EmpID = empId,
                        Status = "Skipped Empty"
                    });

                    continue;
                }

                if (empId.Any(char.IsLetter))
                {
                    results.Add(new
                    {
                        EmpID = empId,
                        Status = "Invalid EmpID"
                    });

                    continue;
                }

                AssignTrainingRequestModel req = new()
                {
                    EmpID = empId,
                    TrainingID = model.TrainingID,
                    Location = model.Location,
                    Start_Date = model.Start_Date,
                    Expiry_Date = model.Expiry_Date,
                    CreatedBy = model.CreatedBy
                };

                int output = _repository.AssignTraining(req);

                results.Add(new
                {
                    EmpID = empId,
                    Status = output == 1
                        ? "Success"
                        : "Failed"
                });
            }

            return results;
        }

        public bool DisableEmployee(DisableEmployeeDto dto)
        {
            return _repository.DisableEmployee(dto);
        }

        public async Task<byte[]?> DownloadReportExcelAsync()
        {
            var data = await _repository.DownloadReportAsync();

            return ExcelHelper.GenerateExcel(data, "Report");
        }
    }
}