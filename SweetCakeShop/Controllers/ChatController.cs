using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Constants;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.Chat;

namespace SweetCakeShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ICustomerProductChatService _customerChat;
        private readonly IAiChatService _adminChat;
        private readonly IConversationMemoryService _adminMemory;

        public ChatController(
            ICustomerProductChatService customerChat,
            IAiChatService adminChat,
            IConversationMemoryService adminMemory)
        {
            _customerChat = customerChat;
            _adminChat = adminChat;
            _adminMemory = adminMemory;
        }

        [HttpGet("GetChatHistory")]
        [AllowAnonymous]
        public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(CancellationToken ct) =>
            Ok(await _customerChat.GetChatHistoryAsync(ct));

        [HttpPost("SendMessage")]
        [AllowAnonymous]
        public async Task<ActionResult<SendChatMessageResponse>> SendMessage(
            [FromBody] SendChatMessageRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new SendChatMessageResponse { Success = false, Reply = "Tin nhắn không hợp lệ." });

            return Ok(await _customerChat.SendMessageAsync(request, ct));
        }

        [HttpGet("customer/suggestions")]
        [AllowAnonymous]
        public ActionResult<object> CustomerSuggestions([FromQuery] int? productId)
        {
            var replies = productId.HasValue
                ? new[] { "Gia mon nay?", "Banh tuong tu?", "Giao hang?", "Chat CSKH" }
                : new[] { "Banh sinh nhat goi y?", "Banh re nhat?", "Giao hang may ngay?", "Chat CSKH" };
            return Ok(new { quickReplies = replies });
        }

        [HttpPost("admin")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public async Task<ActionResult<ChatApiResponse>> Admin(
            [FromBody] ChatApiRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ChatApiResponse { Success = false, Reply = "Tin nhắn không hợp lệ." });

            var reply = await _adminChat.GetAdminReplyAsync(request.Message, cancellationToken);
            return Ok(new ChatApiResponse { Success = true, Reply = reply });
        }

        [HttpPost("admin/clear")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public IActionResult ClearAdminSession()
        {
            _adminMemory.Clear(AiChatMode.Admin);
            return Ok(new { success = true });
        }
    }
}
