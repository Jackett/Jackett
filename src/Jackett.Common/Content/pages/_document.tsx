import Document, { DocumentContext, DocumentInitialProps } from "next/document";
import { Fragment } from "react";
import { CssBaseline } from "@geist-ui/core";

class MyDocument extends Document {
	static async getInitialProps(
		ctx: DocumentContext
	): Promise<DocumentInitialProps> {
		const initialProps = await Document.getInitialProps(ctx);
		const styles = CssBaseline.flush();

		return {
			...initialProps,
			styles: [
				<Fragment key="1">
					{initialProps.styles}
					{styles}
				</Fragment>,
			],
		};
	}
}

export default MyDocument;
