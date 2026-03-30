using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.BudgetTransactions;
using ZansiHustle.Application.BudgetTransactions.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for budget transaction management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetTransactionsController : ControllerBase
    {
        private readonly IBudgetTransactionService _budgetTransactionService;

        /// <summary>
        /// Creates a new instance of the <see cref="BudgetTransactionsController"/> class.
        /// </summary>
        public BudgetTransactionsController(IBudgetTransactionService budgetTransactionService)
        {
            _budgetTransactionService = budgetTransactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _budgetTransactionService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _budgetTransactionService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBudgetTransactionRequestDto request)
        {
            var result = await _budgetTransactionService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetTransactionRequestDto request)
        {
            var result = await _budgetTransactionService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _budgetTransactionService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
