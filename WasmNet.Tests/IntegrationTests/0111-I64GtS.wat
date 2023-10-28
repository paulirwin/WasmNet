;; invoke: gts
;; expect: (i32:1)

(module
    (func (export "gts") (result i32)
        i64.const 42
        i64.const -42
        i64.gt_s
    )
)