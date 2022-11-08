import { Grid } from "@geist-ui/core";

export default function Container({ children }: React.PropsWithChildren<{}>) {
	return (
		<Grid.Container justify="center" marginTop="1.5rem">
			<Grid lg={14}>{children}</Grid>
		</Grid.Container>
	);
}
