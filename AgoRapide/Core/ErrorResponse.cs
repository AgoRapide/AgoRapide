// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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