import { expiredSessionResponse, responseFromApi } from "@/lib/auth/response";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function GET() {
  const result = await sessionApiRequest("users/summary");
  if (result.expired) {
    return expiredSessionResponse();
  }
  return responseFromApi(result.response, result.tokens);
}
