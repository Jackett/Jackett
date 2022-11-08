import { Loading } from "@geist-ui/core";
import { useCache } from "../api/cache";
import Dashboard from "../components/Dashboard";
import Error from "../components/Error";

export default function Cache() {
	const { data, loading, error } = useCache();

	return (
		<Dashboard>
			{error ? (
				<Error>Cache is unavailable, try again later.</Error>
			) : null}
			{loading ? <Loading>Loading cache</Loading> : null}
		</Dashboard>
	);
}
