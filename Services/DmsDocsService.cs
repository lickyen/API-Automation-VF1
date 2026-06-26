using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class DmsDocsService
    {
        private readonly DmsDocsRepository _repository;

        public DmsDocsService(DmsDocsRepository repository)
        {
            _repository = repository;
        }

        // ── Add LANs ──────────────────────────────────────────

        public async Task<DmsDocsAddResult> AddLansAsync(DmsDocsAddRequest request)
        {
            var result = new DmsDocsAddResult();

            if (request.Lans == null || !request.Lans.Any())
            {
                result.Errors.Add("No LANs provided.");
                return result;
            }

            foreach (var lan in request.Lans)
            {
                if (string.IsNullOrWhiteSpace(lan.LAN))
                {
                    result.Errors.Add("Empty LAN skipped.");
                    continue;
                }

                try
                {
                    var output = await _repository.AddLanAsync(lan, request.CreatedBy);
                    if (output == 1)
                        result.RowsAdded++;
                    else if (output == -1)
                        result.Errors.Add($"LAN {lan.LAN}: Already exists.");
                    else
                        result.Errors.Add($"LAN {lan.LAN}: Could not be added.");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"LAN {lan.LAN}: {ex.Message}");
                }
            }

            return result;
        }

        // ── Get LANs ──────────────────────────────────────────
        public async Task<List<DmsDocsListItem>> GetLansAsync()
        {
            var items = await _repository.GetLansAsync();
            return items.ToList();
        }

        // ── Update Status ─────────────────────────────────────

        public async Task<(int Result, string? Error)> UpdateStatusAsync(DmsDocsUpdateStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DocStatus))
                return (0, "DocStatus is required.");

            var result = await _repository.UpdateStatusAsync(request);

            return result == 1
                ? (result, null)
                : (0, "Record not found or no changes made.");
        }
    }
}