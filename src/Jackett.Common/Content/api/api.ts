import useSWR from "swr";
import fetcher from "./swr";

export const base = "/api/v2.0";

export const relevantCategories = [
	"Audio",
	"Books",
	"Console",
	"Movies",
	"Other",
	"PC",
	"TV",
	"XXX",
];

export function useAPI<Type>(route: string, update: boolean) {
	const { data, error } = useSWR<Type>(base + route, fetcher, {
		revalidateIfStale: update,
		revalidateOnFocus: update,
		revalidateOnReconnect: true,
	});

	return {
		data: data,
		loading: !error && !data,
		error: error,
	};
}

export function post(route: string, data: any) {
	return fetcher(base + route, {
		method: "POST",
		headers: {
			"Content-Type": "application/json",
		},
		body: JSON.stringify(data),
	});
}

export function remove(route: string) {
	return fetcher(base + route, {
		method: "DELETE",
	});
}
