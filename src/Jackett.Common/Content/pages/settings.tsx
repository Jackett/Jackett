import {
	Button,
	Fieldset,
	Checkbox,
	Grid,
	Input,
	Select,
	Spacer,
	Spinner,
	Text,
	Link,
	Badge,
	Divider,
	useToasts,
	Snippet,
	Loading,
} from "@geist-ui/core";
import React from "react";
import { useState } from "react";
import {
	saveAdminPassword,
	saveServerConfig,
	useServerConfig,
} from "../api/config";
import updateServer from "../api/update";
import Dashboard from "../components/Dashboard";
import Error from "../components/Error";
import InfoTooltip from "../components/InfoTooltip";
import Label from "../components/Label";

export default function Settings() {
	const { data, loading, error } = useServerConfig(false);
	const [saving, setSaving] = useState(false);
	const [password, setPassword] = useState("");

	const { setToast } = useToasts();

	const save = () => {
		if (data != undefined) {
			setSaving(true);
			if (data.port < 1 || data.port > 65535) {
				data.port = 9117;
			}
			if (password.length > 0) {
				// TODO: Display error if applicable
				saveAdminPassword(password).finally(() => setSaving(false));
				setPassword("");
			}
			console.log(data);
			// TODO: Display error if applicable
			saveServerConfig(data).finally(() => setSaving(false));
		}
	};

	const update = () => {
		setToast({ text: "Update queued.", type: "success" });
		updateServer();
	};

	const forceUpdate = React.useReducer(() => ({}), {})[1] as () => void;

	return (
		<Dashboard>
			{error ? (
				<Error>Settings are unavailable, try again later.</Error>
			) : null}
			{loading ? <Loading>Loading settings</Loading> : null}
			{data ? (
				<Grid.Container gap={2} justify="center">
					<Grid xs={20}>
						<Fieldset width="100%">
							<form>
								<Text h3>Server settings</Text>
								<Input
									width="100%"
									placeholder="9117"
									htmlType="tel"
									initialValue={`${data.port}`}
									onChange={(e) =>
										(data.port =
											Number(e.target.value) || 9117)
									}
								>
									Server port
								</Input>
								<Spacer h={0.5} />
								<Checkbox
									checked={data.external}
									onChange={(e) =>
										(data.external = e.target.checked)
									}
								>
									Allow external access
								</Checkbox>
								<Spacer h={0.5} />
								<Input
									width="100%"
									placeholder="C:\torrents\"
									clearable
									initialValue={data.blackholedir || ""}
									onChange={(e) =>
										(data.blackholedir = e.target.value)
									}
								>
									Blackhole directory
								</Input>
								<Spacer h={0.5} />
								<Checkbox
									checked={data.cors}
									onChange={(e) =>
										(data.cors = e.target.checked)
									}
								>
									Allow CORS
								</Checkbox>
								<Spacer h={0.5} />
								<Checkbox
									checked={data.logging}
									onChange={(e) =>
										(data.logging = e.target.checked)
									}
								>
									Enhanced logging
								</Checkbox>
								<Divider h={0.5} />
								<Checkbox
									checked={!data.updatedisabled}
									onChange={(e) =>
										(data.updatedisabled =
											!e.target.checked)
									}
								>
									Automatic updates
								</Checkbox>
								<Spacer h={0.5} />
								<Checkbox
									checked={data.prerelease}
									onChange={(e) =>
										(data.prerelease = e.target.checked)
									}
								>
									Use pre-release branch
								</Checkbox>
								<Spacer h={0.5} />
								<Button
									auto
									scale={1 / 2}
									type="success"
									onClick={update}
								>
									Update server
								</Button>
							</form>
							<Fieldset.Footer>
								<span>
									<Link
										href="https://github.com/Jackett/Jackett"
										target="_blank"
										icon
									>
										Jackett
									</Link>{" "}
									version {data.app_version}
								</span>
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
						<Spacer h={2} />
						<Fieldset width="100%">
							<form>
								<Text h3>Admin password</Text>
								<Input.Password
									required
									width="100%"
									initialValue={password || "************"}
									onChange={(e) =>
										setPassword(e.target.value)
									}
								>
									Admin password
								</Input.Password>
							</form>
							<Fieldset.Footer>
								<Button
									auto
									scale={1 / 2}
									type="secondary"
									marginLeft="auto"
									loading={saving}
									onClick={save}
								>
									Save
								</Button>
							</Fieldset.Footer>
						</Fieldset>
						<Spacer h={2} />
						<Fieldset width="100%">
							<Text h3>API settings</Text>
							<Label>API key</Label>
							<Snippet
								width="100%"
								symbol=""
								text={data.api_key}
							/>
							<Spacer h={0.5} />
							<Input
								width="100%"
								clearable
								initialValue={data.flaresolverrurl || ""}
								onChange={(e) =>
									(data.flaresolverrurl = e.target.value)
								}
							>
								FlareSolverr API key
							</Input>
							<Spacer h={0.5} />
							<Input
								width="100%"
								htmlType="tel"
								initialValue={`${data.flaresolverr_maxtimeout}`}
								onChange={(e) =>
									(data.flaresolverr_maxtimeout =
										Number(e.target.value) || 55000)
								}
							>
								FlareSolverr max timeout (ms)
							</Input>
							<Spacer h={0.5} />
							<Input
								width="100%"
								clearable
								initialValue={data.omdbkey || ""}
								onChange={(e) =>
									(data.omdbkey = e.target.value)
								}
							>
								OMDB API key
							</Input>
							<Spacer h={0.5} />
							<Input
								width="100%"
								clearable
								htmlType="url"
								initialValue={data.omdburl || ""}
								onChange={(e) =>
									(data.omdburl = e.target.value)
								}
							>
								OMDB API URL
							</Input>
							<Fieldset.Footer>
								<Link
									href="https://github.com/Jackett/Jackett#configuring-flaresolverr"
									target="_blank"
									icon
								>
									FlareSolverr / OMDb documentation
								</Link>
								<Button
									auto
									scale={1 / 2}
									type="secondary"
									marginLeft="auto"
									loading={saving}
									onClick={save}
								>
									Save
								</Button>
							</Fieldset.Footer>
						</Fieldset>
						<Spacer h={2} />
						<Fieldset width="100%">
							<form>
								<Text h3>Proxy settings</Text>
								<Select
									initialValue={`${data.proxy_type}`}
									onChange={(val) => {
										data.proxy_type = Number(val);
										forceUpdate();
									}}
								>
									<Select.Option value="-1">
										Disabled
									</Select.Option>
									<Select.Option value="0">
										HTTP
									</Select.Option>
									<Select.Option value="1">
										SOCKS4
									</Select.Option>
									<Select.Option value="2">
										SOCKS5
									</Select.Option>
								</Select>
								{data.proxy_type > -1 ? (
									<div>
										<Spacer h={0.5} />
										<Input
											width="100%"
											clearable
											initialValue={data.proxy_url || ""}
											onChange={(e) =>
												(data.proxy_url =
													e.target.value)
											}
										>
											Proxy URL
										</Input>
										<Spacer h={0.5} />
										<Input
											width="100%"
											htmlType="tel"
											initialValue={`${
												data.proxy_port || ""
											}`}
											onChange={(e) =>
												(data.proxy_port =
													Number(e.target.value) ||
													3128)
											}
										>
											Proxy port
										</Input>
										<Spacer h={0.5} />
										<Input
											width="100%"
											clearable
											initialValue={
												data.proxy_username || ""
											}
											onChange={(e) =>
												(data.proxy_username =
													e.target.value)
											}
										>
											Proxy username
										</Input>
										<Spacer h={0.5} />
										<Input
											width="100%"
											clearable
											initialValue={
												data.proxy_password || ""
											}
											onChange={(e) =>
												(data.proxy_password =
													e.target.value)
											}
										>
											Proxy password
										</Input>
									</div>
								) : null}
							</form>
							<Fieldset.Footer>
								<Button
									auto
									scale={1 / 2}
									type="secondary"
									marginLeft="auto"
									loading={saving}
									onClick={save}
								>
									Save
								</Button>
							</Fieldset.Footer>
						</Fieldset>
						<Spacer h={2} />
						<Fieldset width="100%" mb="2rem">
							<form>
								<Text h3>Cache settings</Text>
								<Badge type="success">Recommended</Badge>
								<Spacer h={0.5} />
								<Checkbox
									checked={data.cache_enabled}
									onChange={(e) => {
										data.cache_enabled = e.target.checked;
										forceUpdate();
									}}
								>
									Cache content
								</Checkbox>
								{data.cache_enabled ? (
									<div>
										<Spacer h={0.5} />
										<Input
											width="100%"
											placeholder="2100"
											htmlType="tel"
											initialValue={`${data.cache_ttl}`}
											onChange={(e) =>
												(data.cache_ttl =
													Number(e.target.value) ||
													2100)
											}
										>
											Cache TTL (seconds)
											<InfoTooltip>
												How long the results can remain
												in the cache
											</InfoTooltip>
										</Input>
										<Spacer h={0.5} />
										<Input
											width="100%"
											placeholder="1000"
											htmlType="tel"
											initialValue={`${data.cache_max_results_per_indexer}`}
											onChange={(e) =>
												(data.cache_max_results_per_indexer =
													Number(e.target.value) ||
													1000)
											}
										>
											Cache max results per indexer
											<InfoTooltip>
												How many results are kept in
												cache for each indexer, limits
												the use of RAM
											</InfoTooltip>
										</Input>
									</div>
								) : null}
							</form>
							<Fieldset.Footer>
								The cache increases search speed and reduces the
								number of requests to torrent sites.
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
					</Grid>
				</Grid.Container>
			) : null}
		</Dashboard>
	);
}
