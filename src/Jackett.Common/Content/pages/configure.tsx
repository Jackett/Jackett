import {
	Button,
	Card,
	Checkbox,
	Fieldset,
	Input,
	Loading,
	Select,
	Spacer,
	Text,
} from "@geist-ui/core";
import { useRouter } from "next/router";
import { useState } from "react";
import { saveIndexerConfig, useIndexerConfig } from "../api/indexers";
import Dashboard from "../components/Dashboard";
import Error from "../components/Error";
import Label from "../components/Label";

export default function Configure() {
	const router = useRouter();
	const id = document.location.hash.slice(1);

	const { data, loading, error } = useIndexerConfig(id);
	const [saving, setSaving] = useState(false);

	const save = () => {
		if (data != undefined) {
			setSaving(true);
			console.log(data);
			// TODO: Display error if applicable
			saveIndexerConfig(id, data).finally(() => {
				setSaving(false);
				router.back();
			});
		}
	};

	return (
		<Dashboard>
			{error ? (
				<Error>Indexer config is unavailable, try again later.</Error>
			) : null}
			{loading ? <Loading>Loading config</Loading> : null}
			{data ? (
				<Fieldset width="100%" mb={2}>
					<Text h3>Configure {id}</Text>
					{data.map((setting) => (
						<>
							{setting.type == "displayinfo" ? (
								<Card>
									<Text h4>{setting.name}</Text>
									<div
										dangerouslySetInnerHTML={{
											__html: setting.value,
										}}
									/>
								</Card>
							) : setting.type == "inputstring" ? (
								<Input
									width="100%"
									initialValue={setting.value}
									onChange={(e) =>
										(data.find(
											(s) => s.id == setting.id
										)!.value = e.target.value)
									}
								>
									{setting.name}
								</Input>
							) : setting.type == "inputbool" ||
							  setting.type == "inputcheckbox" ? (
								<Checkbox
									checked={setting.value}
									onChange={(e) =>
										(data.find(
											(s) => s.id == setting.id
										)!.value = e.target.checked)
									}
								>
									{setting.name}
								</Checkbox>
							) : setting.type == "inputselect" ? (
								<>
									<Label>{setting.name}</Label>
									<Select
										initialValue={setting.value}
										onChange={(val) =>
											(data.find(
												(s) => s.id == setting.id
											)!.value = val)
										}
									>
										{Object.entries(setting.options).map(
											(option) => (
												<Select.Option
													value={option[0]}
													key={option[0]}
												>
													{option[1] as string}
												</Select.Option>
											)
										)}
									</Select>
								</>
							) : (
								<Input
									width="100%"
									initialValue={setting.value}
									onChange={(e) =>
										(data.find(
											(s) => s.id == setting.id
										)!.value = e.target.value)
									}
								>
									{setting.name}
								</Input>
							)}
							<Spacer h={0.5} />
						</>
					))}
					<Fieldset.Footer>
						<Button
							auto
							scale={1 / 2}
							type="secondary"
							onClick={() => router.back()}
						>
							Back
						</Button>
						<Button
							auto
							scale={1 / 2}
							type="secondary"
							loading={saving}
							onClick={save}
						>
							Save
						</Button>
					</Fieldset.Footer>
				</Fieldset>
			) : null}
		</Dashboard>
	);
}
