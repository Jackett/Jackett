import {
	Badge,
	Button,
	ButtonDropdown,
	Grid,
	Input,
	Link,
	Loading,
	Pagination,
	Table,
	useClipboard,
	useModal,
	useToasts,
} from "@geist-ui/core";
import {
	Activity,
	AlertTriangle,
	Check,
	Plus,
	Search,
	Tool,
	Trash,
} from "@geist-ui/icons";
import { useRouter } from "next/router";
import { useEffect, useState } from "react";
import { relevantCategories } from "../api/api";
import { ServerConfig, useServerConfig } from "../api/config";
import { getPotatoFeed, getRSSFeed, getTorznabFeed } from "../api/feed";
import {
	deleteIndexer,
	Indexer,
	testIndexer,
	useIndexers,
} from "../api/indexers";
import Dashboard from "../components/Dashboard";
import DeleteIndexerModal from "../components/DeleteIndexerModal";
import Error from "../components/Error";
import InfoTooltip from "../components/InfoTooltip";

export default function Indexers() {
	const { data, loading, error } = useIndexers();
	const serverConfig = useServerConfig(true);

	const router = useRouter();

	const [tableData, setTableData] = useState<Indexer[]>([]);
	const [itemsPerPage, setItemsPerPage] = useState(10);
	const [page, setPage] = useState(1);
	const [maxPage, setMaxPage] = useState(1);

	const [query, setQuery] = useState("");

	const { setToast } = useToasts();
	const { copy } = useClipboard();

	const [testing, setTesting] = useState(false);

	const modal = useModal();
	const [pendingDeletion, setPendingDeletion] = useState("");

	useEffect(() => {
		if (data != undefined) {
			const configured = data.filter((indexer) => indexer.configured);
			let filtered = configured.concat(
				data.filter((indexer) => !indexer.configured)
			);
			if (query.length > 0) {
				filtered = data.filter((indexer) =>
					indexer.name.toLowerCase().startsWith(query.toLowerCase())
				);
			}
			const len = filtered.length > 0 ? filtered.length : data.length;
			setMaxPage(Math.round(len / itemsPerPage));
			if (page > maxPage) {
				setPage(1);
			}
			const start = (page - 1) * itemsPerPage;
			const end = start + itemsPerPage;
			setTableData(
				(filtered.length > 0 ? filtered : data).slice(start, end)
			);
		} else {
			setPage(1);
			setMaxPage(1);
		}
	}, [data, page, maxPage, itemsPerPage, query]);

	const remove = (id: string) => {
		deleteIndexer(id).finally(() =>
			setTableData(tableData.filter((row) => row.id != id))
		);
	};

	const test = (id: string, ignoreState?: boolean) => {
		const i = data?.findIndex((indexer) => indexer.id == id) || -1;
		if (i < 0) return Promise.resolve();
		if (!ignoreState) {
			setTesting(true);
		}
		data![i].last_error = "testing";
		return testIndexer(id)
			.then((res) => {
				data![i].last_error =
					res.result != "error" ? "test_success" : "test_failed";
				if (res.result == "error" && res.error != "") {
					const action = {
						name: "report issue",
						handler: () =>
							window.open(
								`https://github.com/Jackett/Jackett/issues/new?template=bug_report.yml&title=${res.error}`,
								"_blank"
							),
					};
					setToast({
						text: res.error,
						type: "error",
						actions: [action],
						delay: 10000,
					});
				}
			})
			.finally(() => {
				if (!ignoreState) {
					setTesting(false);
				}
			});
	};

	const testAll = () => {
		const indexers = data?.filter((indexer) => indexer.configured);
		if (indexers == undefined) return Promise.resolve();
		setTesting(true);
		return Promise.allSettled(
			indexers.map((indexer) => test(indexer.id, true))
		).finally(() => setTesting(false));
	};

	const renderName = (value: any, rowData: Indexer) => {
		return (
			<span>
				<Link href={rowData.site_link} target="_blank" icon>
					{value}
					{rowData.description.length > 0 ? (
						<InfoTooltip>{rowData.description}</InfoTooltip>
					) : null}
				</Link>
				<Badge
					type={
						rowData.type == "private"
							? "error"
							: rowData.type == "semi-private"
							? "warning"
							: "success"
					}
					ml={1 / 2}
				>
					{rowData.type.charAt(0).toUpperCase() +
						rowData.type.slice(1)}
				</Badge>
				<Badge type="secondary" ml={1 / 2}>
					{rowData.language}
				</Badge>
			</span>
		);
	};

	const copyWithToast = (text: string, name: string) => {
		copy(text);
		setToast({ text: `${name} Feed copied.`, type: "success" });
	};

	const renderFeeds = (_: any, rowData: Indexer) => {
		return (
			<ButtonDropdown
				type={rowData.configured ? "secondary" : "default"}
				disabled={!rowData.configured}
			>
				<ButtonDropdown.Item
					main
					onClick={() =>
						copyWithToast(getTorznabFeed(rowData), "Torznab")
					}
				>
					Copy Torznab Feed
				</ButtonDropdown.Item>
				<ButtonDropdown.Item
					onClick={() =>
						copyWithToast(
							getRSSFeed(
								rowData,
								serverConfig?.data || ({} as ServerConfig)
							),
							"RSS"
						)
					}
				>
					Copy RSS Feed
				</ButtonDropdown.Item>
				{rowData.potatoenabled ? (
					<ButtonDropdown.Item
						onClick={() =>
							copyWithToast(getPotatoFeed(rowData), "Potato")
						}
					>
						Copy Potato Feed
					</ButtonDropdown.Item>
				) : null}
			</ButtonDropdown>
		);
	};

	const renderCategories = (_: any, rowData: Indexer) => {
		return (
			<span>
				{rowData.caps
					.filter((cap) => relevantCategories.includes(cap.Name))
					.map((cap) => cap.Name)
					.join(", ")}
			</span>
		);
	};

	const renderActions = (_: any, rowData: Indexer) => {
		if (!rowData.configured) {
			return (
				<Button
					shadow
					type="secondary"
					icon={<Plus />}
					onClick={() => router.push(`/configure#${rowData.id}`)}
				>
					Add indexer
				</Button>
			);
		}
		return (
			<>
				<Button
					type="success"
					ghost
					icon={<Search />}
					auto
					scale={2 / 3}
					px={0.6}
					ml={1 / 2}
					onClick={() => router.push(`/search#${rowData.id}`)}
				/>
				<Button
					type="secondary"
					ghost
					icon={<Tool />}
					auto
					scale={2 / 3}
					px={0.6}
					ml={1 / 2}
					onClick={() => router.push(`/configure#${rowData.id}`)}
				/>
				<Button
					type="error"
					ghost
					icon={<Trash />}
					auto
					scale={2 / 3}
					px={0.6}
					ml={1 / 2}
					onClick={() => {
						setPendingDeletion(rowData.id);
						modal.setVisible(true);
					}}
				/>
				<Button
					type="warning"
					ghost
					icon={
						rowData.last_error == "" ? (
							<Activity />
						) : rowData.last_error == "test_success" ? (
							<Check />
						) : (
							<AlertTriangle />
						)
					}
					auto
					scale={2 / 3}
					px={0.6}
					ml={1 / 2}
					loading={rowData.last_error == "testing"}
					onClick={() => test(rowData.id)}
				/>
			</>
		);
	};

	return (
		<Dashboard>
			{error ? (
				<Error>Indexers are unavailable, try again later.</Error>
			) : null}
			<Grid.Container width="100%" mb={1}>
				<Grid xs={12}>
					<Input
						placeholder="Search for indexers..."
						width="100%"
						scale={4 / 3}
						value={query}
						onChange={(e) => setQuery(e.target.value)}
						icon={<Search />}
						autoFocus
					/>
				</Grid>
				<Grid xs={12} justify="flex-end">
					<Button
						shadow
						type="warning"
						ghost
						icon={<Activity />}
						loading={testing}
						onClick={testAll}
					>
						Test Indexers
					</Button>
				</Grid>
			</Grid.Container>
			<Table data={tableData}>
				<Table.Column prop="name" label="indexer" render={renderName} />
				<Table.Column
					prop="caps"
					label="categories"
					render={renderCategories}
				/>
				<Table.Column prop="id" label="feeds" render={renderFeeds} />
				<Table.Column
					prop="type"
					label="actions"
					render={renderActions}
				/>
			</Table>
			{loading ? <Loading>Loading indexers</Loading> : null}
			<Grid.Container justify="center" mt={2}>
				<Pagination page={page} count={maxPage} onChange={setPage} />
			</Grid.Container>
			<DeleteIndexerModal
				bindings={modal.bindings}
				setVisible={modal.setVisible}
				remove={() => {
					modal.setVisible(false);
					remove(pendingDeletion);
				}}
			/>
		</Dashboard>
	);
}
