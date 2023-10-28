;; invoke: gts
;; expect: (i32:0)

(module
    (func (export "gts") (result i32)
        i32.const 42
        i32.const 42
        i32.gt_s
    )
)