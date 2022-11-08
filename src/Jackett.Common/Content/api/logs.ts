import { useAPI } from "./api";

export type LogLevel = "Trace" | "Debug" | "Info" | "Warn" | "Error" | "Fatal";

export type LogEntry = {
	Level: LogLevel;
	Message: string;
	When: string;
};

export default function useLogs() {
	return useAPI<LogEntry[]>("/server/logs", true);
}
