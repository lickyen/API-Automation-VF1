using ClosedXML.Excel;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;

namespace ProductivityApi.Services
{
    public class CersaiService
    {
        private readonly CersaiRepository _repository;

        public CersaiService(CersaiRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CersaiModel>?> GetPendingAsync(string type)
        {
            return type.ToUpper() switch
            {
                "CC" => await _repository.GetPendingCCAsync(),
                "CS" => await _repository.GetPendingCSAsync(),
                _ => throw new ArgumentException("Invalid Input", nameof(type))
            };
        }

        public async Task<int> ChargeCreationAsync(ChargeCreationDto dto)
        {
            var result = await _repository.ChargeCreationAsync(dto);
            return result;
        }

        public async Task<int> ChargeSatisfactionAsync(ChargeSatisfactionDto dto)
        {
            var result = await _repository.ChargeSatisfactionAsync(dto);
            return result;
        }

        public async Task<int> ChargeCreationBulkAsync(List<ChargeCreationDto> dtos)
        {
            int processed = 0;
            foreach (var dto in dtos)
            {
                var result = await _repository.ChargeCreationAsync(dto);
                if (result > 0) processed++;
            }
            return processed;
        }

        public async Task<int> ChargeSatisfactionBulkAsync(List<ChargeSatisfactionDto> dtos)
        {
            int processed = 0;
            foreach (var dto in dtos)
            {
                var result = await _repository.ChargeSatisfactionAsync(dto);
                if (result > 0) processed++;
            }
            return processed;
        }

        public async Task<byte[]?> GenerateReportExcelAsync()
        {
            var data = await _repository.GetReportDataAsync();
            return ExcelHelper.GenerateExcel(data, "Data");
        }

    }
}
