namespace AgoRapide.Core {
    public class ErrorResponse {
        public ResultCode ResultCode { get; private set; }
        public string Message { get; private set; }
        public ErrorResponse(ResultCode resultCode, string message) {
            ResultCode = resultCode;
            Message = message;
        }
    }
}