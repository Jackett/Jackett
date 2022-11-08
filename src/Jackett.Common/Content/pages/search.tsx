import {
	AutoComplete,
	Button,
	Checkbox,
	Grid,
	Input,
	Table,
	Text,
} from "@geist-ui/core";
import { Filter, Search as SearchIcon } from "@geist-ui/icons";
import { useState } from "react";
import Dashboard from "../components/Dashboard";
import InfoTooltip from "../components/InfoTooltip";

export default function Search() {
	const [query, setQuery] = useState("");
	const [filter, setFilter] = useState("");

	const search = () => {};

	return (
		<Dashboard>
			<Grid.Container width="100%" mb={1}>
				<Grid xs={12}>
					<Input
						placeholder="Search for torrents..."
						width="100%"
						scale={4 / 3}
						value={query}
						onChange={(e) => setQuery(e.target.value)}
						icon={<SearchIcon />}
						autoFocus
					/>
				</Grid>
				<Grid xs={12} justify="flex-end">
					<Button type="secondary">Search</Button>
				</Grid>
				<Grid xs={24} alignItems="center" mt={1}>
					<Text b mb={1}>
						Results per tracker: --
					</Text>
				</Grid>
				<Grid xs={7}>
					<AutoComplete
						placeholder="Filter search results..."
						width="100%"
						scale={4 / 3}
						value={filter}
						onChange={(val) => setFilter(val)}
						// @ts-ignore
						icon={<Filter />}
					/>
				</Grid>
				<Grid xs={1 / 2}>
					<InfoTooltip>
						Narrow down cached search results with a second query.
						Try regex!
					</InfoTooltip>
				</Grid>
				<Grid xs={6} alignItems="center">
					<Checkbox scale={1.5} ml={1 / 2}>
						Show dead torrents
					</Checkbox>
				</Grid>
			</Grid.Container>
			<Table>
				<Table.Column prop="age" label="age" />
				<Table.Column prop="tracker" label="tracker" />
				<Table.Column prop="name" label="name" />
				<Table.Column prop="size" label="size" />
				<Table.Column prop="files" label="# files" />
				<Table.Column prop="category" label="category" />
				<Table.Column prop="grabs" label="grabs" />
				<Table.Column prop="seeds" label="seeds" />
				<Table.Column prop="leeches" label="leeches" />
				<Table.Column prop="dlfactor" label="dl factor" />
				<Table.Column prop="ulfactor" label="ul factor" />
				<Table.Column prop="actions" label="actions" />
			</Table>
		</Dashboard>
	);
}
