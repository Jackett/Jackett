import { post } from "./api";

export default function updateServer() {
	return post("/server/update", {});
}
