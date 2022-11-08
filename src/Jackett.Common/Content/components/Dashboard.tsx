import { Divider, Text } from "@geist-ui/core";
import Container from "./Container";
import Navbar from "./Navbar";

export default function Dashboard({ children }: React.PropsWithChildren<{}>) {
	return (
		<>
			<Container>
				<Text h2>Jackett</Text>
			</Container>

			<Navbar
				pages={[
					{ name: "indexers", keybind: "i" },
					{ name: "search", keybind: "s" },
					{ name: "cache", keybind: "c" },
					{ name: "logs", keybind: "l" },
					{ name: "settings", keybind: "x" },
					{ name: "logout" },
				]}
			/>

			<Divider marginTop="-0.625rem" />

			<Container>{children}</Container>
		</>
	);
}
