import { Grid, Keyboard, Tabs, useKeyboard, useTabs } from "@geist-ui/core";
import { useRouter } from "next/router";

type NavbarPage = {
	name: string;
	keybind?: string;
};

interface NavbarProps {
	pages: NavbarPage[];
}

export default function Navbar({ pages }: NavbarProps) {
	const router = useRouter();
	const currentPage = `${pages.findIndex(
		(page) => page.name == router.pathname.slice(1)
	)}`;
	const { setState, bindings } = useTabs(currentPage);

	bindings.onChange = (i) => {
		router.push(`/${pages[Number(i)].name}`);
	};

	useKeyboard(
		(e) => {
			if (
				!e.ctrlKey &&
				!e.altKey &&
				!e.metaKey &&
				!e.shiftKey &&
				document.activeElement?.tagName !== "INPUT"
			) {
				const i = `${pages.findIndex(
					(page) => page.keybind?.toLowerCase() == e.key
				)}`;
				setState(i);
				bindings.onChange(i);
				e.preventDefault();
			}
		},
		pages
			.filter((page) => page.keybind != undefined)
			.map((page) => page.keybind!.toUpperCase().charCodeAt(0)),
		{ preventDefault: false }
	);

	return (
		<Grid.Container justify="center" marginBottom={0}>
			<Grid lg={14}>
				<Tabs hideDivider leftSpace="0" width="100%" {...bindings}>
					{pages.map((page, i) => (
						<Tabs.Item
							label={
								<>
									{page.name}
									{page.keybind ? (
										<Keyboard ml={1 / 3}>
											{page.keybind.charAt(0)}
										</Keyboard>
									) : null}
								</>
							}
							value={`${i}`}
							key={i}
						/>
					))}
				</Tabs>
			</Grid>
		</Grid.Container>
	);
}
