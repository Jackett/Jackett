import { Loading, Spacer, Table, Text } from "@geist-ui/core";
import useLogs, { LogEntry } from "../api/logs";
import Dashboard from "../components/Dashboard";
import Error from "../components/Error";

export default function Logs() {
	const { data, loading, error } = useLogs();

	const renderMessage = (value: string, rowData: LogEntry) => {
		return (
			<Text
				type={
					rowData.Level == "Warn"
						? "warning"
						: rowData.Level == "Error" || rowData.Level == "Fatal"
						? "error"
						: "default"
				}
			>
				{value}
			</Text>
		);
	};

	return (
		<Dashboard>
			{error ? (
				<Error>Logs are unavailable, try again later.</Error>
			) : null}
			<Table data={data}>
				<Table.Column prop="When" label="when" />
				<Table.Column prop="Level" label="level" />
				<Table.Column
					prop="Message"
					label="message"
					render={renderMessage}
				/>
			</Table>
			{loading ? <Loading>Loading logs</Loading> : null}
			<Spacer h={2} />
		</Dashboard>
	);
}
